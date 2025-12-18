using Microsoft.AspNetCore.Mvc;
using RSSAIzer.Web.Models.ViewModels;
using RSSAIzer.Web.Pages.Shared;
using RSSAIzer.Web.Services;

namespace RSSAIzer.Web.Pages.Digests;

using Microsoft.AspNetCore.Authorization;

[Authorize]
public sealed class IndexModel(BackendClient backend) : BasePageModel
{
    public List<DigestSummaryViewModel> Digests { get; set; } = new();

    public async Task OnGetAsync()
    {
        var result = await backend.GetDigestSummaries();
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return;
        }
        Digests = result.Value.OrderByDescending(d => d.CreatedAt).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var result = await backend.DeleteDigest(id);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return RedirectToPage();
        }
        SuccessMessage = "Digest deleted successfully";
        return RedirectToPage();
    }
}
