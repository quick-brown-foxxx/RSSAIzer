using System.ServiceModel.Syndication;
using System.Text;
using FluentResults;
using RSSAIzer.Backend.Features.DigestFromRssGeneration;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Features;

internal interface IRssPublishingService
{
    /// <summary>
    /// Generates RSS feed containing recent digests
    /// </summary>
    /// <returns>RSS feed as string in XML format</returns>
    Task<Result<SyndicationFeed>> GenerateRssFeed(CancellationToken ct);
}

internal sealed class RssPublishingService(
    IDigestService digestService,
    ILogger<RssPublishingService> logger
) : IRssPublishingService
{
    private const string FEED_BASE_URL = "https://your-app-url"; // TODO: Move to settings

    public async Task<Result<SyndicationFeed>> GenerateRssFeed(CancellationToken ct)
    {
        try
        {
            var digestsResult = await digestService.GetAllDigests(ct);
            if (digestsResult.IsFailed)
            {
                return Result.Fail(digestsResult.Errors);
            }

            var feed = new SyndicationFeed
            {
                Title = new("RSSAIzer Digest Feed"),
                Description = new("Daily summaries of your followed feeds"),
                Language = "en-us",
                BaseUri = new(FEED_BASE_URL),
                Generator = "RSSAIzer Digest RSS Generator",
                Copyright = new($"Copyleft {DateTime.UtcNow.Year}"),
                LastUpdatedTime = DateTime.UtcNow,
            };
            feed.Links.Add(
                new(new($"{FEED_BASE_URL}/rss"), "alternate", default, default, default)
            );

            var items = digestsResult
                .Value.OrderByDescending(d => d.DigestSummary.CreatedAt)
                .Take(50) // Limit to last 50 digests
                .Select(CreateSyndicationItem)
                .ToList();

            feed.Items = items;

            return Result.Ok(feed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate RSS feed");
            return Result.Fail(new Error("RSS feed generation failed").CausedBy(ex));
        }
    }

    private static SyndicationItem CreateSyndicationItem(DigestModel digest)
    {
        const int MaxContentLength = (int)(10 * 1024 / 1.5); // 10kb in UTF8
        var summary = digest.DigestSummary;
        var content = new StringBuilder();
        content.AppendLine(summary.PostsSummary);
        content.AppendLine();
        content.AppendLine($"Posts count: {summary.PostsCount}");
        content.AppendLine($"Average importance: {summary.AverageImportance:F1}/10");
        content.AppendLine();
        content.AppendLine("Individual post summaries:");

        var testContent = new StringBuilder(content.ToString());
        foreach (var post in digest.PostsSummaries.OrderByDescending(p => p.Importance.Number))
        {
            var postContent =
                $"- {post.Summary} (Importance: {post.Importance}/10)\n  Link: {post.Url}\n\n";
            if (testContent.Length + postContent.Length > MaxContentLength)
            {
                content.AppendLine("... (content truncated due to size limit)");
                break;
            }
            content.AppendLine($"- {post.Summary} (Importance: {post.Importance}/10)");
            content.AppendLine($"  Link: {post.Url}");
            content.AppendLine();
            testContent.Append(postContent);
        }

        var item = new SyndicationItem
        {
            Id = summary.DigestId.ToString(),
            Title = new(summary.Title),
            Content = new TextSyndicationContent(content.ToString()),
            PublishDate = summary.CreatedAt,
            LastUpdatedTime = summary.CreatedAt,
        };

        // Add the link to the web UI for this digest
        item.Links.Add(
            new(
                new($"{FEED_BASE_URL}/digest/{summary.DigestId}"),
                "alternate",
                "View Digest",
                "text/html",
                0
            )
        );

        return item;
    }
}
