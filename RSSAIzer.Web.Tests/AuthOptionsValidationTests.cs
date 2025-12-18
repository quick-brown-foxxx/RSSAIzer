using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using RSSAIzer.Web.Options;

namespace RSSAIzer.Web.Tests;

public sealed class AuthOptionsValidationTests
{
    // Helper to run validation and return result
    private static ValidationResult? Validate(AuthOptions options)
    {
        var ctx = new ValidationContext(options);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, ctx, results, true);
        return results.Count == 0 ? ValidationResult.Success : results[0];
    }

    // csharpier-ignore
    [TestCase( true, "00000000-0000-0000-0000-000000000001", "user1@mail.com", null, null, null, null, null, true)] // Valid SingleUser
    // csharpier-ignore
    [TestCase( true, "00000000-0000-0000-0000-000000000001", null, null, null, null, null, null, false)] // Missing SingleUserEmail
    // csharpier-ignore
    [TestCase( true, "00000000-0000-0000-0000-000000000001", "user1@mail.com", "oidc", null, null, null, null, false)] // OIDC set in SingleUser
    // csharpier-ignore
    [TestCase( true, "00000000-0000-0000-0000-000000000001", "user1@mail.com", null, null, null, "proxy@email", null, false)] // SingleUser + Proxy invalid
    [TestCase(true, null, "user1@mail.com", null, null, null, null, null, false)] // Missing SingleUserId
    [TestCase(false, null, null, "oidc", "id", "secret", null, null, true)] // Valid OIDC
    [TestCase(false, null, null, "oidc", null, "secret", null, null, false)] // OIDC missing ClientId
    [TestCase(false, null, null, "oidc", "id", null, null, null, false)] // OIDC missing ClientSecret
    [TestCase(false, null, null, "oidc", "id", "secret", "proxy@email", null, false)] // OIDC + Proxy invalid
    [TestCase(false, null, null, null, null, null, "proxy@email", "proxyid", true)] // Valid Proxy
    [TestCase(false, null, null, null, null, null, null, null, false)] // All fields empty/false
    [TestCase(false, null, null, "oidc", "id", "secret", null, "proxyid", false)] // OIDC + ProxyId invalid
    public void BasicValidationScenarios(
        bool singleUserMode,
        string? singleUserId,
        string? singleUserEmail,
        string? openIdAuthority,
        string? openIdClientId,
        string? openIdClientSecret,
        string? reverseProxyHeaderEmail,
        string? reverseProxyHeaderId,
        bool isValid
    )
    {
        var options = new AuthOptions
        {
            SingleUserMode = singleUserMode,
            SingleUserId = singleUserId == null ? null : Guid.Parse(singleUserId),
            SingleUserEmail = singleUserEmail,
            OpenIdAuthority = openIdAuthority,
            OpenIdClientId = openIdClientId,
            OpenIdClientSecret = openIdClientSecret,
            ReverseProxyHeaderEmail = reverseProxyHeaderEmail,
            ReverseProxyHeaderId = reverseProxyHeaderId,
        };

        var result = Validate(options);
        if (isValid)
        {
            result.Should().Be(ValidationResult.Success);
        }
        else
        {
            result.Should().NotBe(ValidationResult.Success);
        }
    }

    [Test]
    public void OnlyIncorrectFieldsAreListed()
    {
        var options = new AuthOptions
        {
            SingleUserMode = true,
            SingleUserId = null, // should trigger error
            SingleUserEmail = "email@localhost",
            OpenIdAuthority = "should-not-be-set", // should trigger error
            OpenIdClientId = null,
            OpenIdClientSecret = null,
            ReverseProxyHeaderEmail = null,
            ReverseProxyHeaderId = null,
        };

        var result = Validate(options);
        result.Should().NotBeNull();
        result!.MemberNames.Should().BeEquivalentTo("SingleUserId", "OpenIdAuthority");
        result.ErrorMessage.Should().Contain("SINGLE_USER_ID must be set");
        result.ErrorMessage.Should().Contain("OPENID_AUTHORITY must not be set");
        result.ErrorMessage.Should().NotContain("SingleUserEmail");
    }

    [Test]
    public void ErrorMessageAggregatesAllFailures()
    {
        var options = new AuthOptions
        {
            SingleUserMode = true,
            SingleUserId = null, // error
            SingleUserEmail = null, // error
            OpenIdAuthority = "should-not-be-set", // error
            OpenIdClientId = "should-not-be-set", // error
            OpenIdClientSecret = "should-not-be-set", // error
            ReverseProxyHeaderEmail = "should-not-be-set", // error
            ReverseProxyHeaderId = "should-not-be-set", // error
        };
        var result = Validate(options);
        result.Should().NotBeNull();
        result!
            .MemberNames.Should()
            .BeEquivalentTo(
                "SingleUserId",
                "SingleUserEmail",
                "OpenIdAuthority",
                "OpenIdClientId",
                "OpenIdClientSecret",
                "ReverseProxyHeaderEmail",
                "ReverseProxyHeaderId"
            );
        result.ErrorMessage.Should().Contain("SINGLE_USER_ID must be set");
        result.ErrorMessage.Should().Contain("SINGLE_USER_EMAIL must be set");
        result.ErrorMessage.Should().Contain("OPENID_AUTHORITY must not be set");
        result.ErrorMessage.Should().Contain("OPENID_CLIENT_ID must not be set");
        result.ErrorMessage.Should().Contain("OPENID_CLIENT_SECRET must not be set");
        result.ErrorMessage.Should().Contain("REVERSE_PROXY_HEADER_EMAIL must not be set");
        result.ErrorMessage.Should().Contain("REVERSE_PROXY_HEADER_ID must not be set");
    }

    [Test]
    public void NoFalsePositivesInErrorFields()
    {
        var options = new AuthOptions
        {
            SingleUserMode = false,
            SingleUserId = null,
            SingleUserEmail = null,
            OpenIdAuthority = "oidc",
            OpenIdClientId = null, // error
            OpenIdClientSecret = "secret",
            ReverseProxyHeaderEmail = null,
            ReverseProxyHeaderId = null,
        };
        var result = Validate(options);
        result.Should().NotBeNull();
        result.MemberNames.Should().BeEquivalentTo("OpenIdClientId");
        result.ErrorMessage.Should().Contain("OPENID_CLIENT_ID must be set");
        result.ErrorMessage.Should().NotContain("SingleUserId");
        result.ErrorMessage.Should().NotContain("ReverseProxyHeaderEmail");
    }
}
