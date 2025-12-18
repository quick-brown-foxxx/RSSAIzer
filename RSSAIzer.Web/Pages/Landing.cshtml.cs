using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RSSAIzer.Web.Pages;

[AllowAnonymous]
public sealed class LandingModel : PageModel
{
    public void OnGet() { }
}
