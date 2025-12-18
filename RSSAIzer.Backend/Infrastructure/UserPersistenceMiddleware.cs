using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace RSSAIzer.Backend.Infrastructure;

/// <summary>
/// Middleware to ensure the current user is persisted in the database on first login.
/// </summary>
internal sealed class UserPersistenceMiddleware(IUserPersistenceService persistenceService)
    : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var user = context.User;

        // Not authenticated
        if (user?.Identity is not { IsAuthenticated: true })
        {
            await next(context);
            return;
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        var emailClaim = user.FindFirst(ClaimTypes.Email);
        if (userIdClaim == null)
        {
            throw new AuthenticationException(
                "Auth is misconfigured or failed: missing user id claim. Early validation did not catch this"
            );
        }
        if (emailClaim == null)
        {
            throw new AuthenticationException(
                "Auth is misconfigured or failed: missing email claim. Early validation did not catch this"
            );
        }
        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new AuthenticationException(
                "Auth is misconfigured or failed: user id claim is not a valid GUID. Early validation did not catch this"
            );
        }

        await persistenceService.EnsureUserExistsAsync(
            userId,
            emailClaim.Value,
            context.RequestAborted
        );
        await next(context);
    }
}
