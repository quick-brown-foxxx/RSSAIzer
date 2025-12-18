using System.ComponentModel.DataAnnotations;
using RuntimeNullables;

namespace RSSAIzer.Backend.Options;

[NullChecks(false)]
internal record TgRssProvidersOptions
{
    [Required(ErrorMessage = "TG_RSS_PROVIDERS option was not set")]
    [ConfigurationKeyName("TG_RSS_PROVIDERS")]
    [JsonString(DisplayName = "TG_RSS_PROVIDERS")]
    public required string TgRssProviders { get; init; }
}
