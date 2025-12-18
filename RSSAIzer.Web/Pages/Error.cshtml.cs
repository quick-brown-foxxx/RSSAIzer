using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RSSAIzer.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public sealed class ErrorModel(ILogger<ErrorModel> logger, IHostEnvironment environment) : PageModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public string? ExceptionMessage { get; set; }
    public bool ShowException =>
        !string.IsNullOrEmpty(ExceptionMessage) && environment.IsDevelopment();
    public string? ErrorCode { get; set; }

    public void OnGet(int? statusCode = null)
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        if (statusCode.HasValue)
        {
            ErrorCode = statusCode.ToString();
            switch (statusCode)
            {
                case 404:
                    ExceptionMessage = "The requested page could not be found.";
                    break;
                case 403:
                    ExceptionMessage = "You don't have permission to access this resource.";
                    break;
                case 500:
                    ExceptionMessage = "An internal server error occurred.";
                    break;
                default:
                    ExceptionMessage = "An error occurred while processing your request.";
                    break;
            }
        }

        var exception = HttpContext
            .Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()
            ?.Error;
        if (exception != null)
        {
            logger.LogError(exception, "An unhandled exception occurred");
            if (environment.IsDevelopment())
            {
                ExceptionMessage = exception.ToString();
            }
        }
    }
}
