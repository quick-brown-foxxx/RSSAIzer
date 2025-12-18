using FluentResults;
using RSSAIzer.Backend.Infrastructure;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Db;

internal interface IFeedsRepository
{
    Task<Result> SaveFeed(FeedModel feed, CancellationToken cancellationToken);
    Task<Result<List<FeedModel>>> LoadFeeds(CancellationToken cancellationToken);
    Task<Result> DeleteFeed(Uri feedUrl, CancellationToken cancellationToken);
}

internal sealed class FeedsRepository(
    ApplicationDbContext dbContext,
    ILogger<FeedsRepository> logger,
    ICurrentUserContext currentUserContext
) : IFeedsRepository
{
    public async Task<Result> SaveFeed(FeedModel feed, CancellationToken cancellationToken)
    {
        try
        {
            var entity = new FeedEntity
            {
                RssUrl = feed.FeedUrl.ToString(),
                Title = feed.Title,
                Description = feed.Description,
                ImageUrl = feed.ImageUrl.ToString(),
                IsDeleted = false,
                UserId = currentUserContext.UserId,
            };

            var existing = await dbContext
                .Feeds.Where(f =>
                    f.UserId == currentUserContext.UserId && f.RssUrl == entity.RssUrl
                )
                .SingleOrDefaultAsync(cancellationToken);
            if (existing != null)
            {
                dbContext.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                await dbContext.Feeds.AddAsync(entity, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save feed [{FeedUrl}]", feed.FeedUrl);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result<List<FeedModel>>> LoadFeeds(CancellationToken cancellationToken)
    {
        try
        {
            var entities = await dbContext
                .Feeds.Where(e => !e.IsDeleted && e.UserId == currentUserContext.UserId)
                .ToListAsync(cancellationToken);

            var feeds = entities
                .Select(e => new FeedModel(
                    FeedUrl: new(e.RssUrl),
                    Title: e.Title,
                    Description: e.Description,
                    ImageUrl: new(e.ImageUrl)
                ))
                .ToList();

            return Result.Ok(feeds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load feeds");
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }

    public async Task<Result> DeleteFeed(Uri feedUrl, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await dbContext
                .Feeds.Where(f =>
                    f.UserId == currentUserContext.UserId && f.RssUrl == feedUrl.ToString()
                )
                .SingleOrDefaultAsync(cancellationToken);
            if (entity == null)
            {
                return Result.Ok(); // Already deleted
            }

            entity.IsDeleted = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete feed [{FeedUrl}]", feedUrl);
            return Result.Fail(new Error("Database operation failed").CausedBy(ex));
        }
    }
}
