using System.ComponentModel.DataAnnotations;

namespace RSSAIzer.Web.Options;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AuthOptionsValidationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not AuthOptions o)
        {
            return ValidationResult.Success;
        }

        var failures = new List<ValidationResult>();

        // SingleUserMode: No OIDC or Proxy fields allowed, other SingleUser fields must be set
        if (o.SingleUserMode)
        {
            if (o.SingleUserId == null)
            {
                failures.Add(
                    new("SINGLE_USER_ID must be set in SingleUserMode.", [nameof(o.SingleUserId)])
                );
            }
            if (string.IsNullOrWhiteSpace(o.SingleUserEmail))
            {
                failures.Add(
                    new(
                        "SINGLE_USER_EMAIL must be set in SingleUserMode.",
                        [nameof(o.SingleUserEmail)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.OpenIdAuthority))
            {
                failures.Add(
                    new(
                        "OPENID_AUTHORITY must not be set in SingleUserMode.",
                        [nameof(o.OpenIdAuthority)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.OpenIdClientId))
            {
                failures.Add(
                    new(
                        "OPENID_CLIENT_ID must not be set in SingleUserMode.",
                        [nameof(o.OpenIdClientId)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.OpenIdClientSecret))
            {
                failures.Add(
                    new(
                        "OPENID_CLIENT_SECRET must not be set in SingleUserMode.",
                        [nameof(o.OpenIdClientSecret)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ReverseProxyHeaderEmail))
            {
                failures.Add(
                    new(
                        "REVERSE_PROXY_HEADER_EMAIL must not be set in SingleUserMode.",
                        [nameof(o.ReverseProxyHeaderEmail)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ReverseProxyHeaderId))
            {
                failures.Add(
                    new(
                        "REVERSE_PROXY_HEADER_ID must not be set in SingleUserMode.",
                        [nameof(o.ReverseProxyHeaderId)]
                    )
                );
            }
        }
        // OIDC Mode: Authority set, must have ClientId/ClientSecret, no Proxy fields
        else if (
            !string.IsNullOrWhiteSpace(o.OpenIdAuthority)
            || !string.IsNullOrWhiteSpace(o.OpenIdClientId)
            || !string.IsNullOrWhiteSpace(o.OpenIdClientSecret)
        )
        {
            if (string.IsNullOrWhiteSpace(o.OpenIdClientId))
            {
                failures.Add(
                    new("OPENID_CLIENT_ID must be set in OIDC mode.", [nameof(o.OpenIdClientId)])
                );
            }
            if (string.IsNullOrWhiteSpace(o.OpenIdClientSecret))
            {
                failures.Add(
                    new(
                        "OPENID_CLIENT_SECRET must be set in OIDC mode.",
                        [nameof(o.OpenIdClientSecret)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ReverseProxyHeaderEmail))
            {
                failures.Add(
                    new(
                        "REVERSE_PROXY_HEADER_EMAIL must not be set in OIDC mode.",
                        [nameof(o.ReverseProxyHeaderEmail)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.ReverseProxyHeaderId))
            {
                failures.Add(
                    new(
                        "REVERSE_PROXY_HEADER_ID must not be set in OIDC mode.",
                        [nameof(o.ReverseProxyHeaderId)]
                    )
                );
            }
        }
        // Proxy Mode: ProxyHeader fields set, no OIDC fields
        else if (
            !string.IsNullOrWhiteSpace(o.ReverseProxyHeaderEmail)
            || !string.IsNullOrWhiteSpace(o.ReverseProxyHeaderId)
        )
        {
            if (!string.IsNullOrWhiteSpace(o.OpenIdAuthority))
            {
                failures.Add(
                    new(
                        "OPENID_AUTHORITY must not be set when using ProxyHeader authentication.",
                        [nameof(o.OpenIdAuthority)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.OpenIdClientId))
            {
                failures.Add(
                    new(
                        "OPENID_CLIENT_ID must not be set when using ProxyHeader authentication.",
                        [nameof(o.OpenIdClientId)]
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(o.OpenIdClientSecret))
            {
                failures.Add(
                    new(
                        "OPENID_CLIENT_SECRET must not be set when using ProxyHeader authentication.",
                        [nameof(o.OpenIdClientSecret)]
                    )
                );
            }
        }
        // If none of the modes apply, must be misconfigured
        else
        {
            failures.Add(
                new(
                    "AuthenticationOptions must be configured for SingleUserMode, OpenIdConnect, or ReverseProxy (headers) authentication."
                )
            );
        }

        if (failures.Count == 0)
        {
            return ValidationResult.Success;
        }

        // Aggregate messages and member names
        var allMessages = string.Join("; ", failures.ConvertAll(f => f.ErrorMessage));
        var allMembers = new List<string>();
        foreach (var f in failures)
        {
            allMembers.AddRange(f.MemberNames);
        }

        return new(allMessages, allMembers);
    }
}
