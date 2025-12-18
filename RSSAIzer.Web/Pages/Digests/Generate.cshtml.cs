using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RSSAIzer.Backend.Models;
using RSSAIzer.Web.Models.ViewModels;
using RSSAIzer.Web.Pages.Shared;
using RSSAIzer.Web.Services;
using RSSAIzer.Web.Utils;

namespace RSSAIzer.Web.Pages.Digests;

using Microsoft.AspNetCore.Authorization;

[Authorize]
public sealed class GenerateModel(BackendClient backend) : BasePageModel
{
    [BindProperty]
    public DigestGenerationViewModel? Form { get; set; }

    public List<FeedViewModel> Feeds { get; private set; } = [];

    public TemplateWithContent? DefaultPostSummaryUserPrompt { get; private set; }
    public TemplateWithContent? DefaultPostImportanceUserPrompt { get; private set; }
    public TemplateWithContent? DefaultDigestSummaryUserPrompt { get; private set; }

    public async Task OnGetAsync()
    {
        var settingsResult = await backend.GetSettings();
        if (settingsResult.IsFailed)
        {
            Errors = settingsResult.Errors;
            return;
        }

        var settings = settingsResult.Value;
        DefaultPostSummaryUserPrompt = settings.PromptPostSummaryUser;
        DefaultPostImportanceUserPrompt = settings.PromptPostImportanceUser;
        DefaultDigestSummaryUserPrompt = settings.PromptDigestSummaryUser;

        var feedsResult = await backend.GetFeeds();
        if (feedsResult.IsFailed)
        {
            Errors = feedsResult.Errors;
            return;
        }

        Feeds = feedsResult.Value;

        // If digest time hasn't passed today, set range to previous day
        // Otherwise set range to today
        var allFeedUrls = Feeds.Select(f => f.Url).ToArray();
        Form = new()
        {
            DateTo = DateTime.Now.ToUniversalTime(),
            DateFrom = DateTime.Now.AddDays(-1).ToUniversalTime(),
            SelectedFeedUrls = allFeedUrls,
            PostSummaryUserPromptOverride = settings.PromptPostSummaryUser,
            PostImportanceUserPromptOverride = settings.PromptPostImportanceUser,
            DigestSummaryUserPromptOverride = settings.PromptDigestSummaryUser,
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var feedsResult = await backend.GetFeeds();
            if (!feedsResult.IsFailed)
            {
                Feeds = feedsResult.Value;
            }

            return Page();
        }

        if (Form == null)
        {
            throw new UnreachableException("Form is null. Error in frontend!");
        }

        var result = await backend.QueueDigest(
            Form.DateFrom,
            Form.DateTo,
            Form.SelectedFeedUrls,
            Form.PostSummaryUserPromptOverride,
            Form.PostImportanceUserPromptOverride,
            Form.DigestSummaryUserPromptOverride
        );
        if (result.IsFailed)
        {
            Errors = result.Errors;
            var feedsResult = await backend.GetFeeds();
            if (!feedsResult.IsFailed)
            {
                Feeds = feedsResult.Value;
            }

            return Page();
        }

        SuccessMessage = "Digest generation queued successfully";
        return RedirectToPage(Routes.Page_DigestProgress, new { id = result.Value });
    }
}
