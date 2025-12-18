using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using RSSAIzer.Web.Options;

namespace RSSAIzer.Web.Infrastructure.Auth;

public static class AuthSetupExtensions
{
    public static IServiceCollection AddAuthenticationCustom(this IServiceCollection services)
    {
        // Build a temporary provider to resolve IOptions<AuthenticationOptions>
        using var sp = services.BuildServiceProvider();
        var authOptions = sp.GetRequiredService<IOptions<AuthOptions>>().Value;

        services.AddAuthorization();

        switch (authOptions.GetMode())
        {
            case AuthMode.SingleUser:
                ConfigureSingleUserAuth(services);
                break;
            case AuthMode.OpenIdConnect:
                ConfigureOidcAuth(services, authOptions);
                break;
            case AuthMode.ReverseProxy:
                ConfigureReverseProxyAuth(services);
                break;
            default:
                throw new UnreachableException($"Unknown auth mode: {authOptions.GetMode()}");
        }

        if (!string.IsNullOrWhiteSpace(authOptions.CookieName))
        {
            services.Configure<CookieAuthenticationOptions>(options =>
                options.Cookie.Name = authOptions.CookieName
            );
        }

        return services;
    }

    private static void ConfigureSingleUserAuth(IServiceCollection services)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = SingleUserAuthHandler.SCHEME_NAME;
                options.DefaultAuthenticateScheme = SingleUserAuthHandler.SCHEME_NAME;
                options.DefaultChallengeScheme = SingleUserAuthHandler.SCHEME_NAME;
            })
            .AddScheme<AuthenticationSchemeOptions, SingleUserAuthHandler>(
                SingleUserAuthHandler.SCHEME_NAME,
                _ => { }
            );
    }

    private static void ConfigureReverseProxyAuth(IServiceCollection services)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = ProxyHeaderHandler.SCHEME_NAME;
                options.DefaultAuthenticateScheme = ProxyHeaderHandler.SCHEME_NAME;
                options.DefaultChallengeScheme = ProxyHeaderHandler.SCHEME_NAME;
            })
            .AddScheme<AuthenticationSchemeOptions, ProxyHeaderHandler>(
                ProxyHeaderHandler.SCHEME_NAME,
                _ => { }
            );
    }

    private static void ConfigureOidcAuth(IServiceCollection services, AuthOptions authOptions)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "oidc";
                options.DefaultAuthenticateScheme = "oidc";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddOpenIdConnect(
                "oidc",
                options =>
                {
                    options.Authority = authOptions.OpenIdAuthority;
                    options.ClientId =
                        authOptions.OpenIdClientId
                        ?? throw new UnreachableException(
                            $"{authOptions.OpenIdClientId} is required for OIDC, auth is"
                                + $" misconfigured,  early validation broke and didn't catch this"
                        );
                    options.ClientSecret =
                        authOptions.OpenIdClientSecret
                        ?? throw new UnreachableException(
                            $"{authOptions.OpenIdClientSecret} is required for OIDC, auth is "
                                + $"misconfigured, early validation broke and didn't catch this"
                        );
                    options.ResponseType = "code";
                    options.SaveTokens = true;
                }
            );
    }
}
