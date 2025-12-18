using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RSSAIzer.Web.Models.ViewModels;
using RSSAIzer.Web.Pages.Shared;
using RSSAIzer.Web.Services;
using RSSAIzer.Web.Utils;

namespace RSSAIzer.Web.Pages.Digest;

using Microsoft.AspNetCore.Authorization;

[Authorize]
public sealed class IndexModel(BackendClient backend) : BasePageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    public DigestViewModel? Summary { get; set; }

    public PostSummaryViewModel[]? Posts { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id != Id)
        {
            throw new UnreachableException(
                "Error in frontend! Id of digest does not match the one in the URL"
            );
        }

        var result = await backend.GetDigest(id);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return Page();
        }

        (Summary, Posts) = result.Value;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var result = await backend.DeleteDigest(id);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            return RedirectToPage(Routes.Page_DigestsIndex);
        }
        SuccessMessage = "Digest deleted successfully";
        return RedirectToPage(Routes.Page_DigestsIndex);
    }
}
