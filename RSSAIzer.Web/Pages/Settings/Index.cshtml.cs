using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RSSAIzer.Web.Models.ViewModels;
using RSSAIzer.Web.Pages.Shared;
using RSSAIzer.Web.Services;

namespace RSSAIzer.Web.Pages.Settings;

using Microsoft.AspNetCore.Authorization;

[Authorize]
public sealed class IndexModel(BackendClient backend) : BasePageModel
{
    [BindProperty]
    public SettingsViewModel? Settings { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var result = await backend.GetSettings();
        if (result.IsFailed)
        {
            Errors = result.Errors;
            Settings = GenerateDefaultSettings();
            return Page();
        }
        Settings = result.Value;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        if (Settings is null)
        {
            throw new UnreachableException("Error in web page! Can't read new settings");
        }

        var result = await backend.UpdateSettings(Settings);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return Page();
        }
        SuccessMessage = "Settings updated successfully";
        return RedirectToPage();
    }

    private static SettingsViewModel GenerateDefaultSettings() =>
        new()
        {
            RecipientEmail = "user@domain.com",
            SmtpHost = new("smtp.gmail.com"),
            SmtpPort = 587,
            SmtpUsername = "username",
            SmtpPassword = "password",
            SmtpUseSsl = true,
            OpenAiApiKey = "key123456789",
            OpenAiModel = "gpt-3.5-turbo",
            OpenAiEndpoint = new("https://api.openai.com/v1"),
            DigestTimeUtc = new(8, 0),
            PromptPostSummaryUser = new("Some prompt here {Content}"),
            PromptPostImportanceUser = new("Some prompt here {Content}"),
            PromptDigestSummaryUser = new("Some prompt here {Content}"),
        };
}
