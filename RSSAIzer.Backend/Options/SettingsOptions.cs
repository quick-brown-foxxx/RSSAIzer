using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RuntimeNullables;

namespace RSSAIzer.Backend.Options;

[NullChecks(false)]
internal sealed record SettingsJson(
    [property: JsonRequired] string EmailRecipient,
    [property: JsonRequired] TimeOnly DigestTime,
    [property: JsonRequired] SmtpSettingsJson SmtpSettings,
    [property: JsonRequired] OpenAiSettingsJson OpenAiSettings,
    [property: JsonRequired] PromptSettingsJson PromptSettings
);

[NullChecks(false)]
internal sealed record PromptSettingsJson(
    [property: JsonRequired] string PostSummaryUserPrompt,
    [property: JsonRequired] string PostImportanceUserPrompt,
    [property: JsonRequired] string DigestSummaryUserPrompt
);

[NullChecks(false)]
internal sealed record SmtpSettingsJson(
    [property: JsonRequired] string Host,
    [property: JsonRequired] int Port,
    [property: JsonRequired] string Username,
    [property: JsonRequired] string Password,
    [property: JsonRequired] bool UseSsl
);

[NullChecks(false)]
internal sealed record OpenAiSettingsJson(
    [property: JsonRequired] string ApiKey,
    [property: JsonRequired] string Model,
    [property: JsonRequired] Uri Endpoint
);

[NullChecks(false)]
internal sealed record SettingsOptions
{
    [Required(ErrorMessage = "DEFAULT_SETTINGS configuration option was not set")]
    [JsonString(DisplayName = "DEFAULT_SETTINGS")]
    [ConfigurationKeyName("DEFAULT_SETTINGS")]
    public required string DefaultSettings { get; init; }
}
