using System.Diagnostics;
using System.ServiceModel.Syndication;
using System.Xml;
using FluentResults;
using FluentValidation;
using RSSAIzer.Backend.Models;
using RSSAIzer.Backend.Utils;

namespace RSSAIzer.Backend.Features.DigestFromRssGeneration;

internal record ReadPostModel(Html HtmlContent, Uri Url, DateTime PublishedAt);

internal interface IFeedReader
{
    Task<Result<List<ReadPostModel>>> FetchPosts(
        FeedUrl feedUrl,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    );
    Task<Result<FeedModel>> FetchFeedInfo(FeedUrl feedUrl, CancellationToken ct);
}

internal sealed class FeedReader(ILogger<FeedReader> logger) : IFeedReader
{
    private const string MissingFeedDescription = "[missing-in-rss-feed]";
    private const string AtomNamespace = "http://www.w3.org/2005/Atom";
    private static readonly Uri DefaultPlaceholderImageUrl = new(
        "https://placehold.co/150x150?text=F"
    );

    private static readonly SyndicationFeedValidator SyndicationFeedValidator = new();
    private static readonly AtomItemValidator AtomItemValidator = new();
    private static readonly RssItemValidator RssItemValidator = new();

    public Task<Result<List<ReadPostModel>>> FetchPosts(
        FeedUrl feedUrl,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    ) =>
        Task.Run(
            () =>
            {
                ct.ThrowIfCancellationRequested();

                if (from > to)
                {
                    return Result.Fail(
                        $"Can't fetch posts, invalid period requested, from: [{from}] to [{to}]"
                    );
                }

                try
                {
                    ct.ThrowIfCancellationRequested();
                    var feedUrlString = feedUrl.Url.ToString();

                    // Detect feed format before loading
                    var feedFormat = DetectFeedFormat(feedUrlString);
                    if (feedFormat == FeedFormat.Unknown)
                    {
                        return Result.Fail(
                            new Error($"Unsupported or unrecognized feed format for {feedUrl}")
                        );
                    }

                    ct.ThrowIfCancellationRequested();
                    using var reader = XmlReader.Create(feedUrlString);
                    var feed = SyndicationFeed.Load(reader);

                    ct.ThrowIfCancellationRequested();
                    var feedValidationResult = SyndicationFeedValidator.Validate(feed);
                    if (!feedValidationResult.IsValid)
                    {
                        var errorMessages = string.Join(
                            "; ",
                            feedValidationResult.Errors.Select(e => e.ErrorMessage)
                        );
                        logger.LogWarning(
                            "Validation failed for feed {FeedUrl}: {Errors}",
                            feedUrl,
                            errorMessages
                        );
                        return Result.Fail(
                            new Error($"Validation failed for feed {feedUrl}: {errorMessages}")
                        );
                    }

                    // Select appropriate validator based on feed format
                    var itemValidator =
                        feedFormat == FeedFormat.Atom
                            ? (IValidator<SyndicationItem>)AtomItemValidator
                            : RssItemValidator;

                    var posts = new List<ReadPostModel>();
                    var validationErrors = new List<Error>();

                    foreach (
                        var item in feed.Items.Where(x =>
                            DateOnly.FromDateTime(x.PublishDate.DateTime) >= from
                            && DateOnly.FromDateTime(x.PublishDate.DateTime) <= to
                        )
                    )
                    {
                        var itemValidationResult = itemValidator.Validate(item);
                        if (!itemValidationResult.IsValid)
                        {
                            var errorMessages = string.Join(
                                "; ",
                                itemValidationResult.Errors.Select(e => e.ErrorMessage)
                            );
                            validationErrors.Add(
                                new Error(
                                    $"Validation failed for feed item [{item.Id}]: {errorMessages}"
                                )
                            );
                            logger.LogWarning(
                                "Validation failed for feed item {ItemId}: {Errors}",
                                item.Id,
                                errorMessages
                            );
                            continue;
                        }

                        try
                        {
                            var alternateLink = item
                                .Links.Where(l => l.RelationshipType == "alternate")
                                .SingleOrDefaultIfNotExactlyOne();

                            // Map content based on feed format
                            string htmlText = feedFormat switch
                            {
                                FeedFormat.Atom => item.Content switch
                                {
                                    TextSyndicationContent textContent => textContent.Text
                                        ?? throw new FormatException(
                                            $"Atom feed item [{item.Id}] has null content text"
                                        ),
                                    null => throw new FormatException(
                                        $"Atom feed item [{item.Id}] has no content"
                                    ),
                                    _ => throw new FormatException(
                                        $"Atom feed item [{item.Id}] has unsupported content type: {item.Content.GetType().Name}"
                                    ),
                                },
                                FeedFormat.Rss20 => item.Summary?.Text
                                    ?? throw new FormatException(
                                        $"RSS feed item [{item.Id}] has no summary"
                                    ),
                                _ => throw new FormatException(
                                    $"Unsupported feed format for item [{item.Id}]"
                                ),
                            };

                            var post = new ReadPostModel(
                                HtmlContent: new(htmlText),
                                Url: alternateLink?.Uri
                                    ?? throw new FormatException(
                                        $"Feed item [{item.Id}] has invalid URLs [{LinksCollectionToString(item.Links)}]"
                                    ),
                                PublishedAt: item.PublishDate.DateTime
                            );

                            posts.Add(post);
                        }
                        catch (FormatException ex)
                        {
                            validationErrors.Add(
                                new Error($"Invalid feed item [{item.Id}]: {ex.Message}").CausedBy(
                                    ex
                                )
                            );
                            logger.LogWarning(ex, "Invalid feed item {ItemId}", item.Id);
                        }
                    }

                    if (validationErrors.Count > 0)
                    {
                        return Result.Fail(validationErrors);
                    }

                    return Result.Ok(posts);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching posts for feed {FeedUrl}", feedUrl);
                    return Result.Fail(
                        new Error($"Error fetching posts for feed {feedUrl}").CausedBy(ex)
                    );
                }
            },
            ct
        );

    public Task<Result<FeedModel>> FetchFeedInfo(FeedUrl feedUrl, CancellationToken ct) =>
        Task.Run(
            () =>
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var feedUrlString = feedUrl.Url.ToString();

                    // Detect feed format before loading
                    var feedFormat = DetectFeedFormat(feedUrlString);
                    if (feedFormat == FeedFormat.Unknown)
                    {
                        return Result.Fail(
                            new Error($"Unsupported or unrecognized feed format for {feedUrl}")
                        );
                    }

                    using var reader = XmlReader.Create(feedUrlString);
                    var feed = SyndicationFeed.Load(reader);

                    ct.ThrowIfCancellationRequested();
                    var feedValidationResult = SyndicationFeedValidator.Validate(feed);
                    if (!feedValidationResult.IsValid)
                    {
                        var errorMessages = string.Join(
                            "; ",
                            feedValidationResult.Errors.Select(e => e.ErrorMessage)
                        );
                        logger.LogWarning(
                            "Validation failed for feed {FeedUrl}: {Errors}",
                            feedUrl,
                            errorMessages
                        );
                        return Result.Fail(
                            new Error($"Validation failed for feed {feedUrl}: {errorMessages}")
                        );
                    }

                    var titleText = feed.Title.Text;

                    var descriptionText = feed.Description?.Text;
                    if (string.IsNullOrWhiteSpace(descriptionText))
                    {
                        descriptionText = MissingFeedDescription;
                        logger.LogDebug(
                            "Feed {FeedUrl} has missing description, using fallback",
                            feedUrl
                        );
                    }

                    // Extract image URL based on feed format
                    var imageUrl = ExtractFeedImageUrl(feed, feedFormat);

                    var feedModel = new FeedModel(
                        FeedUrl: feedUrl,
                        Description: descriptionText,
                        Title: titleText,
                        ImageUrl: imageUrl
                    );

                    return Result.Ok(feedModel);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching feed info for {FeedUrl}", feedUrl);
                    return Result.Fail(new Error("Failed to fetch feed info").CausedBy(ex));
                }
            },
            ct
        );

    private static FeedFormat DetectFeedFormat(string feedUrl)
    {
        try
        {
            // fetch xml content and rea
            using var reader = XmlReader.Create(feedUrl);
            reader.MoveToContent();
            var rootElement = reader.LocalName;
            var rootNamespace = reader.NamespaceURI;

            if (rootElement == "feed" && rootNamespace == AtomNamespace)
            {
                return FeedFormat.Atom;
            }
            else if (rootElement == "rss")
            {
                return FeedFormat.Rss20;
            }

            return FeedFormat.Unknown;
        }
        catch
        {
            return FeedFormat.Unknown;
        }
    }

    private static Uri ExtractFeedImageUrl(SyndicationFeed feed, FeedFormat feedFormat)
    {
        return feedFormat switch
        {
            FeedFormat.Atom => feed.ImageUrl
                ?? feed.Links.FirstOrDefault(l => l.RelationshipType == "logo")?.Uri
                ?? feed.Links.FirstOrDefault(l => l.RelationshipType == "icon")?.Uri
                ?? DefaultPlaceholderImageUrl,
            FeedFormat.Rss20 => feed.ImageUrl ?? DefaultPlaceholderImageUrl,
            _ => throw new UnreachableException("Incorrect feed format. This should never happen."),
        };
    }

    private static string LinksCollectionToString(IEnumerable<SyndicationLink> links) =>
        string.Join(", ", links.Select(link => $"{link.Uri} ({link.RelationshipType})"));
}
