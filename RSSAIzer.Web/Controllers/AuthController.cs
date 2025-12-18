using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RSSAIzer.Web.Infrastructure.Auth;
using RSSAIzer.Web.Options;

namespace RSSAIzer.Web.Controllers;

[Route("Auth")]
public sealed class AuthController(IOptions<AuthOptions> authOptions) : Controller
{
    private readonly AuthOptions _authOptions = authOptions.Value;

    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_authOptions.GetMode() == AuthMode.SingleUser)
        {
            HttpContext.Session.SetString(
                SingleUserAuthHandler.LOGIN_COOKIE,
                SingleUserAuthHandler.LOGIN_COOKIE_VALUE
            );
            return Redirect(returnUrl ?? Url.Content("~/"));
        }
        return _authOptions.GetMode() switch
        {
            AuthMode.OpenIdConnect => Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl ?? Url.Content("~/") },
                OpenIdConnectDefaults.AuthenticationScheme
            ),
            _ => Redirect(returnUrl ?? Url.Content("~/")),
        };
    }

    [HttpGet("Logout")]
    public IActionResult Logout(string? returnUrl = null)
    {
        if (_authOptions.GetMode() == AuthMode.SingleUser)
        {
            HttpContext.Session.Remove(SingleUserAuthHandler.LOGIN_COOKIE);
            return Redirect(Url.Content("~/"));
        }

        if (_authOptions.GetMode() == AuthMode.ReverseProxy)
        {
            if (string.IsNullOrWhiteSpace(_authOptions.ReverseProxyLogoutUrl))
            {
                throw new UnreachableException(
                    $"No {_authOptions.ReverseProxyLogoutUrl} for reverse proxy mode. Auth is misconfigured and early validation did not catch it"
                );
            }

            return Redirect(_authOptions.ReverseProxyLogoutUrl);
        }

        if (_authOptions.GetMode() == AuthMode.OpenIdConnect)
        {
            return SignOut(
                new AuthenticationProperties { RedirectUri = returnUrl ?? Url.Content("~/") },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            );
        }

        throw new UnreachableException($"Unknown auth mode: {_authOptions.GetMode()}");
    }
}
