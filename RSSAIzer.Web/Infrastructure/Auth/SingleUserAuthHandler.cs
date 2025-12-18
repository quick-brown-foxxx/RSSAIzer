using System.Diagnostics;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using RSSAIzer.Web.Options;

namespace RSSAIzer.Web.Infrastructure.Auth;

public sealed class SingleUserAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AuthOptions> authOptions
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SCHEME_NAME = "SingleUser";
    public const string LOGIN_COOKIE = "SingleUserLoggedIn";
    public const string LOGIN_COOKIE_VALUE = "true";

    private readonly AuthOptions _authOptions = authOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Only authenticate if session/cookie flag is set
        var loggedIn = Context.Session.GetString(LOGIN_COOKIE);
        if (string.IsNullOrEmpty(loggedIn) || loggedIn != LOGIN_COOKIE_VALUE)
        {
            // Not logged in: unauthenticated
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Use configured SingleUserId and SingleUserEmail, or sensible defaults
        var userId =
            _authOptions.SingleUserId
            ?? throw new UnreachableException(
                $"{nameof(_authOptions.SingleUserId)} must be set for SingleUserMode, early validation broke and did not catch it"
            );
        var userEmail = !string.IsNullOrWhiteSpace(_authOptions.SingleUserEmail)
            ? _authOptions.SingleUserEmail
            : throw new UnreachableException(
                $"{nameof(_authOptions.SingleUserEmail)} must be set for SingleUserMode, early validation broke and did not catch it"
            );
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, userEmail),
        };
        var identity = new ClaimsIdentity(claims, SCHEME_NAME);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SCHEME_NAME);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // Redirect to landing page instead of returning 401
        Context.Response.Redirect("/Landing");
        return Task.CompletedTask;
    }
}
