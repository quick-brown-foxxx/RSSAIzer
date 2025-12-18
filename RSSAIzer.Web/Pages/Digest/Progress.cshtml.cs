using Microsoft.AspNetCore.Mvc;
using RSSAIzer.Web.Models.ViewModels;
using RSSAIzer.Web.Pages.Shared;
using RSSAIzer.Web.Services;

namespace RSSAIzer.Web.Pages.Digest;

using Microsoft.AspNetCore.Authorization;

[Authorize]
public sealed class ProgressModel(BackendClient backend) : BasePageModel
{
    public DigestProgressViewModel? Progress { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var result = await backend.GetDigestProgress(id);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return Page();
        }

        Progress = result.Value;
        return Page();
    }
}
