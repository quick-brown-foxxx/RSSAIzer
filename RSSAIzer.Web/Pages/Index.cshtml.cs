using RSSAIzer.Web.Models.ViewModels;
using RSSAIzer.Web.Pages.Shared;
using RSSAIzer.Web.Services;

namespace RSSAIzer.Web.Pages;

using Microsoft.AspNetCore.Authorization;

[Authorize]
public sealed class IndexModel(BackendClient backend) : BasePageModel
{
    public DigestSummaryViewModel? LatestDigest { get; set; }
    public int TotalFeeds { get; set; }
    public int TotalDigests { get; set; }
    public DateTime? NextDigestTime { get; set; }
    public List<FeedViewModel> RandomFeeds { get; set; } = [];

    public async Task OnGetAsync()
    {
        // Get latest digest
        var digestsResult = await backend.GetDigestSummaries();
        if (digestsResult.IsFailed)
        {
            Errors = digestsResult.Errors;
            return;
        }
        var digests = digestsResult.Value;
        LatestDigest = digests.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
        TotalDigests = digests.Count;

        // Get feeds info
        var feedsResult = await backend.GetFeeds();
        if (feedsResult.IsFailed)
        {
            Errors = feedsResult.Errors;
            return;
        }
        var feeds = feedsResult.Value;
        TotalFeeds = feeds.Count;
        RandomFeeds = feeds.OrderByDescending(_ => Random.Shared.Next()).Take(5).ToList();

        // Get next digest time from settings
        var settingsResult = await backend.GetSettings();
        if (settingsResult.IsFailed)
        {
            Errors = settingsResult.Errors;
            return;
        }
        var settings = settingsResult.Value;
        NextDigestTime = DateTime.UtcNow.Date.Add(settings.DigestTimeUtc.ToTimeSpan());
        if (NextDigestTime < DateTime.UtcNow)
        {
            NextDigestTime = NextDigestTime.Value.AddDays(1);
        }
    }
}
