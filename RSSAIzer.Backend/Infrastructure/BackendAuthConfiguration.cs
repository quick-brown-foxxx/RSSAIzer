using System.Diagnostics.CodeAnalysis;

namespace RSSAIzer.Backend.Infrastructure;

public enum AuthenticationMode
{
    SingleUser,
    ReverseProxy,
    OpenIdConnect,
}

/// <summary>
/// Contains the authentication configuration that backend needs to know.
/// </summary>
public sealed record BackendAuthConfiguration
{
    public BackendAuthConfiguration(
        string? proxyHeaderId,
        AuthenticationMode mode,
        Guid? singleUserId,
        string? singleUserEmail
    )
    {
        ProxyHeaderId = proxyHeaderId;
        Mode = mode;
        SingleUserId = singleUserId;
        SingleUserEmail = singleUserEmail;

        if (mode == AuthenticationMode.SingleUser && singleUserId == null)
        {
            throw new ArgumentException(
                $"{nameof(SingleUserId)} cannot be null in single user mode"
            );
        }
        if (mode == AuthenticationMode.SingleUser && string.IsNullOrWhiteSpace(singleUserEmail))
        {
            throw new ArgumentException(
                $"{nameof(SingleUserEmail)} cannot be null or whitespace in single user mode"
            );
        }
        if (mode == AuthenticationMode.ReverseProxy && string.IsNullOrWhiteSpace(proxyHeaderId))
        {
            throw new ArgumentException(
                $"{nameof(ProxyHeaderId)} cannot be null or whitespace in reverse proxy mode"
            );
        }
    }

    /// <summary>Name of the HTTP header containing the user's unique ID, for reverse proxy mode.</summary>
    public string? ProxyHeaderId { get; }

    /// <summary>Current authentication mode.</summary>
    public AuthenticationMode Mode { get; }

    /// <summary>User ID to use in single user mode.</summary>
    public Guid? SingleUserId { get; }

    /// <summary>Email to use in single user mode.</summary>
    public string? SingleUserEmail { get; }

    [MemberNotNullWhen(true, nameof(SingleUserId))]
    public bool IsSingleUserMode => Mode == AuthenticationMode.SingleUser;

    [MemberNotNullWhen(true, nameof(ProxyHeaderId))]
    public bool IsReverseProxyMode => Mode == AuthenticationMode.ReverseProxy;
}
