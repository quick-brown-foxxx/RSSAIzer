using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RSSAIzer.Backend.Models;
using RuntimeNullables;

namespace RSSAIzer.Web.Models.ViewModels;

[NullChecks(false)]
public sealed class DigestGenerationViewModel : IValidatableObject
{
    [Display(Name = "Post Summary User Prompt Override")]
    [ModelBinder(typeof(TemplateWithContentModelBinder))]
    public TemplateWithContent? PostSummaryUserPromptOverride { get; init; }

    [Display(Name = "Post Importance User Prompt Override")]
    [ModelBinder(typeof(TemplateWithContentModelBinder))]
    public TemplateWithContent? PostImportanceUserPromptOverride { get; init; }

    [Display(Name = "Digest Summary User Prompt Override")]
    [ModelBinder(typeof(TemplateWithContentModelBinder))]
    public TemplateWithContent? DigestSummaryUserPromptOverride { get; init; }

    [Display(Name = "From Date")]
    public required DateTime DateFrom { get; init; }

    [Display(Name = "To Date")]
    public required DateTime DateTo { get; init; }

    [Display(Name = "Selected Feed Urls")]
    public string[] SelectedFeedUrls { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateTo <= DateFrom)
        {
            yield return new(
                "End date must be after start date",
                [nameof(DateTo), nameof(DateFrom)]
            );
        }

        if (
            DateFrom.ToUniversalTime() > DateTime.UtcNow
            || DateTo.ToUniversalTime() > DateTime.UtcNow
        )
        {
            yield return new(
                "Cannot generate digest for future dates",
                [nameof(DateTo), nameof(DateFrom)]
            );
        }

        if (SelectedFeedUrls.Length == 0)
        {
            yield return new("At least one feed must be selected", [nameof(SelectedFeedUrls)]);
        }
    }
}
