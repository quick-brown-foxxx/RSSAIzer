using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSSAIzer.Web.Pages.Shared;
using RSSAIzer.Web.Services;

namespace RSSAIzer.Web.Pages.Digests;

[Authorize]
public sealed class QueueModel(BackendClient backend) : BasePageModel
{
    public Guid[] InProgressDigests { get; private set; } = [];
    public Guid[] WaitingDigests { get; private set; } = [];
    public Guid[] CancellationRequestedDigests { get; private set; } = [];

    public async Task OnGetAsync()
    {
        InProgressDigests = await backend.GetInProgressDigests();
        WaitingDigests = await backend.GetWaitingDigests();
        CancellationRequestedDigests = await backend.GetCancellationRequestedDigests();
    }

    public async Task<IActionResult> OnPostCancelDigestAsync(Guid digestId)
    {
        var result = await backend.CancelDigest(digestId);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            ModelState.AddModelError(string.Empty, "Failed to cancel digest");
        }
        else
        {
            SuccessMessage = "Digest cancellation requested successfully";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveWaitingDigestAsync(Guid digestId)
    {
        var result = await backend.RemoveWaitingDigest(digestId);
        if (result.IsFailed)
        {
            Errors = result.Errors;
            ModelState.AddModelError(string.Empty, "Failed to remove waiting digest");
        }
        else
        {
            SuccessMessage = "Waiting digest removed successfully";
        }
        return RedirectToPage();
    }
}
