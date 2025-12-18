using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RSSAIzer.Web.Pages.Shared;

public class BasePageModel : PageModel
{
    [TempData]
    public string? SuccessMessage { get; set; }

    [ViewData]
    public List<IError>? Errors { get; set; }
}
