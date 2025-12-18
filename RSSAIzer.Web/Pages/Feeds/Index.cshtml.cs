using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSSAIzer.Web.Models.ViewModels;
using RSSAIzer.Web.Pages.Shared;
using RSSAIzer.Web.Services;

namespace RSSAIzer.Web.Pages.Feeds;

public sealed record FeedWithMetadata(FeedViewModel Feed, bool IsTelegramFeed)
{
    public static FeedWithMetadata FromFeedViewModel(FeedViewModel feed) =>
        new(feed, feed.Url.StartsWith("https://t.me/", StringComparison.OrdinalIgnoreCase));
}

[Authorize]
public sealed class IndexModel(BackendClient backend) : BasePageModel
{
    public List<FeedWithMetadata>? Feeds { get; set; }

    public async Task OnGetAsync()
    {
        var result = await backend.GetFeeds();
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return;
        }
        Feeds = result
            .Value.OrderBy(c => c.Title)
            .Select(FeedWithMetadata.FromFeedViewModel)
            .ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string feedUrl)
    {
        var result = await backend.DeleteFeed(feedUrl);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return RedirectToPage();
        }
        SuccessMessage = "Feed deleted successfully";
        return RedirectToPage();
    }
}
