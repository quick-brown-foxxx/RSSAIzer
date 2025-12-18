using System.Diagnostics;
using System.Text.Json;
using FluentResults;
using RSSAIzer.Backend.Infrastructure;
using RSSAIzer.Backend.Models;
using RSSAIzer.Shared;

namespace RSSAIzer.Backend.Db;

internal interface IDigestRepository
{
    /// <summary>
    /// Loads complete digest including all post summaries and metadata
    /// </summary>
    public Task<Result<DigestModel?>> LoadDigestForCurrentUser(
        DigestId digestId,
        CancellationToken ct
    );

    /// <summary>
    /// Saves a digest to the database
    /// </summary>
    public Task<Result> SaveDigestForCurrentUser(DigestModel digest, CancellationToken ct);

    /// <summary>
    /// Loads all digest summaries from the database (without posts)
    /// </summary>
    public Task<Result<DigestSummaryModel[]>> LoadAllDigestSummariesForCurrentUser(
        CancellationToken ct
    );

    /// <summary>
    /// Loads all digests including all post summaries and metadata
    /// </summary>
    public Task<Result<DigestModel[]>> LoadAllDigestsForCurrentUser(CancellationToken ct);

    /// <summary>
    /// Deletes a digest and all associated posts.
    /// </summary>
    public Task<Result> DeleteDigestForCurrentUser(DigestId digestId, CancellationToken ct);
}

internal sealed class DigestRepository(
    ApplicationDbContext dbContext,
    ILogger<DigestRepository> logger,
    ICurrentUserContext currentUserContext
) : IDigestRepository
{
    public async Task<Result<DigestModel?>> LoadDigestForCurrentUser(
        DigestId digestId,
        CancellationToken ct
    )
    {
        try
        {
            // Include both PostsNav and SummaryNav to get complete digest data
            var digest = await dbContext
                .Digests.Include(d => d.PostsNav)!
                    .ThenInclude(p => p.FeedNav)
                .Include(d => d.SummaryNav)
                .Where(d => d.UserId == currentUserContext.UserId)
                .SingleOrDefaultAsync(d => d.Id == digestId.Guid, ct);

            if (digest is null)
            {
                return Result.Ok<DigestModel?>(null);
            }
            if (digest.SummaryNav == null || digest.PostsNav == null)
            {
                return Result.Fail(new Error($"Failed to load digest {digestId} from database"));
            }

            return Result.Ok<DigestModel?>(MapToModel(digest));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load digest {DigestId}", digestId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result> SaveDigestForCurrentUser(DigestModel digest, CancellationToken ct)
    {
        try
        {
            var entity = MapToEntity(digest, currentUserContext.UserId);
            await dbContext.Digests.AddAsync(entity, ct);
            await dbContext.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save digest {DigestId}", digest.DigestId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result<DigestSummaryModel[]>> LoadAllDigestSummariesForCurrentUser(
        CancellationToken ct
    )
    {
        try
        {
            var summaries = await dbContext
                .Digests.Where(d => d.UserId == currentUserContext.UserId)
                .Select(d => d.SummaryNav)
                .WhereNotNull()
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(ct);

            return Result.Ok(summaries.Select(MapToSummaryModel).ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load digest summaries");
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result<DigestModel[]>> LoadAllDigestsForCurrentUser(CancellationToken ct)
    {
        try
        {
            var digests = await dbContext
                .Digests.Include(d => d.PostsNav)!
                    .ThenInclude(p => p.FeedNav)
                .Include(d => d.SummaryNav)
                .Where(d => d.UserId == currentUserContext.UserId)
                .ToListAsync(ct);

            if (digests.Any(d => d.SummaryNav == null || d.PostsNav == null))
            {
                return Result.Fail(new Error("Failed to load digests from database"));
            }

            return Result.Ok(digests.Select(MapToModel).ToArray());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load digests");
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result> DeleteDigestForCurrentUser(DigestId digestId, CancellationToken ct)
    {
        try
        {
            var digest = await dbContext
                .Digests.Include(d => d.PostsNav) // Load the related posts
                .Include(d => d.SummaryNav) // Load the related summary
                .Where(d => d.UserId == currentUserContext.UserId)
                .SingleOrDefaultAsync(d => d.Id == digestId.Guid, ct);

            if (digest == null)
            {
                return Result.Fail(new Error($"Failed to load digest {digestId}"));
            }
            if (digest.SummaryNav == null)
            {
                return Result.Fail(new Error($"Failed to load summary for digest {digestId}"));
            }
            if (digest.PostsNav == null)
            {
                return Result.Fail(new Error($"Failed to load posts for digest {digestId}"));
            }

            // Remove the digest
            // DigestSummary and related posts should be deleted cascade
            dbContext.Digests.Remove(digest);

            // Ensure related posts and summary are deleted:
            dbContext.DigestSummaries.Remove(digest.SummaryNav);
            dbContext.PostSummaries.RemoveRange(digest.PostsNav);

            await dbContext.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete digest {DigestId}", digestId);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    private static DigestModel MapToModel(DigestEntity entity)
    {
        if (entity.SummaryNav == null || entity.PostsNav == null)
        {
            throw new ArgumentException("Digest entity is incomplete", nameof(entity));
        }

        var allFeeds = entity.PostsNav.Select(p => (p.FeedNav, p.Id)).ToArray();
        var firstPostWithNullFeed = allFeeds.FirstOrDefault(p => p.FeedNav == null);
        if (firstPostWithNullFeed.FeedNav != null)
        {
            throw new InvalidOperationException(
                $"In Digest entity {entity.Id}, Post {firstPostWithNullFeed.Id} FeedNav is null"
            );
        }

        var feedCache = new Dictionary<string, FeedModel>();
        return new(
            DigestId: new(entity.Id),
            PostsSummaries:
            [
                .. entity.PostsNav.Select(p =>
                {
                    if (p.FeedNav == null)
                    {
                        throw new InvalidOperationException("FeedNav is null");
                    }

                    var feedUrl = p.FeedNav.RssUrl;
                    if (!feedCache.TryGetValue(feedUrl, out var feedModel))
                    {
                        feedModel = new(
                            new(p.FeedNav.RssUrl),
                            p.FeedNav.Description,
                            p.FeedNav.Title,
                            new(p.FeedNav.ImageUrl)
                        );
                        feedCache[feedUrl] = feedModel;
                    }
                    return new PostSummaryModel(
                        Feed: feedModel,
                        Summary: p.Summary,
                        Url: new(p.Url),
                        PublishedAt: p.PublishedAt,
                        Importance: new(p.Importance)
                    );
                }),
            ],
            DigestSummary: MapToSummaryModel(entity.SummaryNav),
            UsedPrompts: JsonSerializer.Deserialize<Dictionary<PromptTypeEnumModel, string>>(
                entity.UsedPrompts
            )
                ?? throw new InvalidOperationException(
                    "Failed to deserialize UsedPrompts for unknow reason"
                )
        );
    }

    private static DigestEntity MapToEntity(DigestModel model, Guid userId)
    {
        var digestEntity = new DigestEntity
        {
            Id = model.DigestId.Guid,
            UserId = userId,
            SummaryNav = new()
            {
                Id = model.DigestId.Guid,
                Title = model.DigestSummary.Title,
                PostsSummary = model.DigestSummary.PostsSummary,
                PostsCount = model.DigestSummary.PostsCount,
                AverageImportance = model.DigestSummary.AverageImportance,
                CreatedAt = model.DigestSummary.CreatedAt,
                DateFrom = model.DigestSummary.DateFrom,
                DateTo = model.DigestSummary.DateTo,
                DigestNav = null, // Will be set by EF Core
            },
            PostsNav =
            [
                .. model.PostsSummaries.Select(p => new PostSummaryEntity
                {
                    Id = Guid.NewGuid(),
                    FeedUrl = p.Feed.FeedUrl.Url.ToString(),
                    Summary = p.Summary,
                    Url = p.Url.ToString(),
                    PublishedAt = p.PublishedAt,
                    Importance = p.Importance.Number,
                    DigestId = model.DigestId.Guid,
                    DigestNav = null, // Will be set by EF Core
                    FeedNav = null, // Will be set by EF Core
                }),
            ],
            UsedPrompts = JsonSerializer.Serialize(model.UsedPrompts),
        };

        return digestEntity;
    }

    private static DigestSummaryModel MapToSummaryModel(DigestSummaryEntity entity) =>
        new(
            DigestId: new(entity.Id),
            Title: entity.Title,
            PostsSummary: entity.PostsSummary,
            PostsCount: entity.PostsCount,
            AverageImportance: entity.AverageImportance,
            CreatedAt: entity.CreatedAt,
            DateFrom: entity.DateFrom,
            DateTo: entity.DateTo
        );

    private static PromptTypeEnumEntity MapPromptTypeToEntity(PromptTypeEnumModel model) =>
        model switch
        {
            PromptTypeEnumModel.PostSummary => PromptTypeEnumEntity.PostSummary,
            PromptTypeEnumModel.PostImportance => PromptTypeEnumEntity.PostImportance,
            PromptTypeEnumModel.DigestSummary => PromptTypeEnumEntity.DigestSummary,
            _ => throw new UnreachableException("Unknown PromptType enum when mapping to entity"),
        };

    private static PromptTypeEnumModel MapPromptTypeToModel(PromptTypeEnumEntity entity) =>
        entity switch
        {
            PromptTypeEnumEntity.PostSummary => PromptTypeEnumModel.PostSummary,
            PromptTypeEnumEntity.PostImportance => PromptTypeEnumModel.PostImportance,
            PromptTypeEnumEntity.DigestSummary => PromptTypeEnumModel.DigestSummary,
            _ => throw new UnreachableException("Unknown PromptType enum when mapping to model"),
        };
}
