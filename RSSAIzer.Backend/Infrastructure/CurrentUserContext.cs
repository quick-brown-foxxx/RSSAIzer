using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace RSSAIzer.Backend.Infrastructure;

internal interface ICurrentUserContext
{
    Guid UserId { get; }
}

internal sealed class CurrentUserContext(
    IHttpContextAccessor httpContextAccessor,
    BackendAuthConfiguration authConfig
) : ICurrentUserContext
{
    public Guid UserId => ResolveUserId();

    private Guid ResolveUserId()
    {
        if (authConfig.IsSingleUserMode)
        {
            return authConfig.SingleUserId.Value;
        }

        var ctx = httpContextAccessor.HttpContext;
        if (ctx?.User?.Identity is not { IsAuthenticated: true })
        {
            throw new AuthenticationException(
                "Auth misconfigured or failed: user is not authenticated or authentication failed for unknown reason"
            );
        }

        if (authConfig.IsReverseProxyMode)
        {
            var proxyId = ctx.Request.Headers[authConfig.ProxyHeaderId].ToString();
            if (!Guid.TryParse(proxyId, out var guidFromHeader))
            {
                throw new AuthenticationException(
                    "Auth misconfigured or failed: proxy header is not a valid GUID"
                );
            }
            return guidFromHeader;
        }

        // OpenID Connect mode
        // Try claim (sub or nameidentifier)
        var claim = ctx.User.FindFirst(ClaimTypes.NameIdentifier) ?? ctx.User.FindFirst("sub");
        if (claim == null)
        {
            throw new AuthenticationException(
                "Auth misconfigured or failed: claim not found for OpenID Connect mode"
            );
        }
        if (Guid.TryParse(claim.Value, out var guidFromClaim))
        {
            return guidFromClaim;
        }

        throw new AuthenticationException(
            "Auth misconfigured or failed: claim is not a valid GUID for OpenID Connect mode"
        );
    }
}
