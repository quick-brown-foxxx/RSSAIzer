using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace RSSAIzer.Backend.Options;

/// <summary>Validates that the target string contains syntactically correct JSON.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
internal sealed class JsonStringAttribute : ValidationAttribute
{
    public string? DisplayName { get; init; }

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is null || string.IsNullOrWhiteSpace($"{value}"))
        {
            return ValidationResult.Success;
        }

        try
        {
            JsonDocument.Parse((string)value);
            return ValidationResult.Success;
        }
        catch (JsonException ex)
        {
            return new($"{DisplayName ?? ctx.DisplayName} must contain a valid JSON. {ex.Message}");
        }
    }
}
