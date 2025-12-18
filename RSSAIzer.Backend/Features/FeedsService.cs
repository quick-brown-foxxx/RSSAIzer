using FluentResults;
using RSSAIzer.Backend.Db;
using RSSAIzer.Backend.Features.DigestFromRssGeneration;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Features;

internal interface IFeedsService
{
    /// <summary>
    /// Adds or updates a feed
    /// </summary>
    public Task<Result> AddOrUpdateFeed(FeedUrl feedUrl, CancellationToken ct);

    /// <summary>
    /// Returns all non-deleted feeds
    /// </summary>
    public Task<Result<List<FeedModel>>> GetFeeds(CancellationToken ct);

    /// <summary>
    /// Marks a feed as deleted (soft delete)
    /// </summary>
    public Task<Result> RemoveFeed(FeedUrl feedUrl, CancellationToken ct);
}

internal sealed class FeedsService(
    IFeedsRepository feedsRepository,
    IFeedReader feedReader,
    ILogger<FeedsService> logger
) : IFeedsService
{
    private readonly ILogger<FeedsService> _logger = logger;

    public async Task<Result> AddOrUpdateFeed(FeedUrl feedUrl, CancellationToken ct)
    {
        var feedResult = await feedReader.FetchFeedInfo(feedUrl, ct);
        if (feedResult.IsFailed)
        {
            return Result.Fail(feedResult.Errors);
        }
        return await feedsRepository.SaveFeed(feedResult.Value, ct);
    }

    public async Task<Result<List<FeedModel>>> GetFeeds(CancellationToken ct)
    {
        var result = await feedsRepository.LoadFeeds(ct);
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }

        return Result.Ok(result.Value.ToList());
    }

    public async Task<Result> RemoveFeed(FeedUrl feedUrl, CancellationToken ct)
    {
        return await feedsRepository.DeleteFeed(feedUrl.Url, ct);
    }
}
