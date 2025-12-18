using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using RuntimeNullables;

namespace RSSAIzer.Web.Options;

public enum AuthMode
{
    SingleUser,
    ReverseProxy,
    OpenIdConnect,
}

/// <summary>
/// Authentication configuration options for RSSAIzer.
/// Controls SingleUser, Reverse Proxy, and OpenID Connect authentication modes.
/// </summary>
[NullChecks(false)]
[AuthOptionsValidation]
public sealed record AuthOptions
{
    /// <summary>
    /// If true, enables dummy local authentication (single-user mode). All other auth fields must be unset.
    /// </summary>
    [Required(ErrorMessage = "SINGLE_USER_MODE configuration option was not set")]
    [ConfigurationKeyName("SINGLE_USER_MODE")]
    public required bool SingleUserMode { get; init; }

    /// <summary>
    /// User ID for SingleUser mode. Required in SingleUser mode.
    /// </summary>
    [ConfigurationKeyName("SINGLE_USER_ID")]
    public Guid? SingleUserId { get; init; }

    /// <summary>
    /// Email for SingleUser mode. Required in SingleUser mode.
    /// </summary>
    [ConfigurationKeyName("SINGLE_USER_EMAIL")]
    public string? SingleUserEmail { get; init; }

    /// <summary>
    /// OpenID Connect authority URL. Required in OpenID Connect mode.
    /// </summary>
    [ConfigurationKeyName("OPENID_AUTHORITY")]
    public string? OpenIdAuthority { get; init; }

    /// <summary>
    /// OpenID Connect client ID. Required in OpenID Connect mode.
    /// </summary>
    [ConfigurationKeyName("OPENID_CLIENT_ID")]
    public string? OpenIdClientId { get; init; }

    /// <summary>
    /// OpenID Connect client secret. Required in OpenID Connect mode.
    /// </summary>
    [ConfigurationKeyName("OPENID_CLIENT_SECRET")]
    public string? OpenIdClientSecret { get; init; }

    /// <summary>
    /// Name of the HTTP header containing the user's email. Required in reverse proxy mode.
    /// </summary>
    [ConfigurationKeyName("REVERSE_PROXY_HEADER_EMAIL")]
    public string? ReverseProxyHeaderEmail { get; init; }

    /// <summary>
    /// Name of the HTTP header containing the user's unique ID. Required in reverse proxy mode.
    /// </summary>
    [ConfigurationKeyName("REVERSE_PROXY_HEADER_ID")]
    public string? ReverseProxyHeaderId { get; init; }

    /// <summary>
    /// External logout URL for Reverse Proxy mode. If set, logout will redirect here.
    /// </summary>
    [ConfigurationKeyName("REVERSE_PROXY_LOGOUT_URL")]
    public string? ReverseProxyLogoutUrl { get; init; }

    /// <summary>
    /// Cookie name for ASP authentication cookie. Optional; default is used if unset. Can be configured for any mode.
    /// </summary>
    [ConfigurationKeyName("COOKIE_NAME")]
    public string? CookieName { get; init; }
}

public static class AuthOptionsExtensions
{
    public static AuthMode GetMode(this AuthOptions options) =>
        options.SingleUserMode ? AuthMode.SingleUser
        : options.OpenIdAuthority is not null ? AuthMode.OpenIdConnect
        : options.ReverseProxyHeaderEmail is not null ? AuthMode.ReverseProxy
        : throw new UnreachableException(
            "Authentication is misconfigured and early validation broke and did not catch it"
        );
}
