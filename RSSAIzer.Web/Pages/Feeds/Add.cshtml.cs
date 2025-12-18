using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using RSSAIzer.Web.Models.ViewModels;
using RSSAIzer.Web.Pages.Shared;
using RSSAIzer.Web.Services;
using RSSAIzer.Web.Utils;
using RuntimeNullables;

namespace RSSAIzer.Web.Pages.Feeds;

[Authorize]
[RequestTimeout(10)]
public sealed class AddModel(BackendClient backend) : BasePageModel
{
    public enum FeedType
    {
        [Display(Name = "Telegram Channel")]
        Telegram,

        [Display(Name = "Raw RSS Feed")]
        RawRss,
    }

    [BindProperty]
    public required FeedType? Type { get; set; }

    [BindProperty]
    public RawRssFeedModel? RawRss { get; set; }

    [BindProperty]
    public TelegramFeedModel? Telegram { get; set; }

    public List<RssProvider>? RssProviders { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        RssProviders = await backend.GetRssProviders(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        // Clear validation errors for the model that wasn't selected
        if (Type == FeedType.RawRss)
        {
            ModelState.Remove("Telegram.ProviderName");
            ModelState.Remove("Telegram.ChannelId");
        }
        else if (Type == FeedType.Telegram)
        {
            ModelState.Remove("RawRss.FeedUrl");
        }

        RssProviders = await backend.GetRssProviders(ct);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var feedUrl = Type switch
        {
            FeedType.RawRss when RawRss != null => RawRss.FeedUrl,
            FeedType.Telegram when Telegram != null => RssProviders
                .Single(p => p.Name == Telegram.ProviderName)
                .BaseUrl + Telegram.ChannelId,
            _ => throw new UnreachableException("Invalid form state"),
        };

        var result = await backend.AddOrUpdateFeed(new() { FeedUrl = feedUrl }, ct);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return Page();
        }

        SuccessMessage = $"Feed '{feedUrl}' added successfully";
        return RedirectToPage(Routes.Page_FeedsIndex);
    }
}

[NullChecks(false)]
public sealed record RawRssFeedModel
{
    [Url]
    [Display(Name = "RSS Feed URL")]
    public required string FeedUrl { get; init; }
}

[NullChecks(false)]
public sealed record TelegramFeedModel
{
    [Display(Name = "RSS Provider")]
    public required string ProviderName { get; init; }

    [Display(Name = "Channel ID")]
    [RegularExpression(
        "^[a-zA-Z][a-zA-Z0-9_]{4,31}$",
        ErrorMessage = "Channel ID must be 5-32 characters, only letters, numbers, and underscores, starting with a letter"
    )]
    public required string ChannelId { get; init; }
}
