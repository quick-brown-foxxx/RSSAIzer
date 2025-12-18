using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;

namespace RSSAIzer.Web.Infrastructure.Auth;

using Microsoft.Extensions.Options;
using Options;

internal sealed class ProxyHeaderHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AuthOptions> authOptions
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SCHEME_NAME = "ProxyHeader";

    private const string EMAIL_HEADER_NAME = "X-Proxy-Email";
    private const string ID_HEADER_NAME = "X-Proxy-Id";

    private readonly AuthOptions _authOptions = authOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var emailHeaderName = _authOptions.ReverseProxyHeaderEmail ?? EMAIL_HEADER_NAME;
        var idHeaderName = _authOptions.ReverseProxyHeaderId ?? ID_HEADER_NAME;
        var emailHeader = Context.Request.Headers[emailHeaderName].ToString();
        var idHeader = Context.Request.Headers[idHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(emailHeader) || string.IsNullOrWhiteSpace(idHeader))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing proxy headers"));
        }
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, idHeader),
            new Claim(ClaimTypes.Email, emailHeader),
        };
        var identity = new ClaimsIdentity(claims, SCHEME_NAME);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SCHEME_NAME);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
