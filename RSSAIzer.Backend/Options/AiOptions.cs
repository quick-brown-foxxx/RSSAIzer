using System.ComponentModel.DataAnnotations;
using RuntimeNullables;

namespace RSSAIzer.Backend.Options;

[NullChecks(false)]
public record AiOptions
{
    [Required(ErrorMessage = "POST_SUMMARY_SYSTEM_PROMPT option was not set")]
    [ConfigurationKeyName("POST_SUMMARY_SYSTEM_PROMPT")]
    public required string PostSummarySystemPrompt { get; init; }

    [Required(ErrorMessage = "POST_IMPORTANCE_SYSTEM_PROMPT option was not set")]
    [ConfigurationKeyName("POST_IMPORTANCE_SYSTEM_PROMPT")]
    public required string PostImportanceSystemPrompt { get; init; }

    [Required(ErrorMessage = "DIGEST_SUMMARY_SYSTEM_PROMPT option was not set")]
    [ConfigurationKeyName("DIGEST_SUMMARY_SYSTEM_PROMPT")]
    public required string DigestSummarySystemPrompt { get; init; }
}
