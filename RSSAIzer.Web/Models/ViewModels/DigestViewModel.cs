using System.ComponentModel.DataAnnotations;
using RuntimeNullables;

namespace RSSAIzer.Web.Models.ViewModels;

[NullChecks(false)]
public sealed record DigestViewModel
{
    public required Guid Id { get; init; }

    [Display(Name = "Created At")]
    public required DateTime CreatedAt { get; init; }

    public required string Summary { get; init; }

    public required string Title { get; init; }

    public required int PostsCount { get; init; }

    public required double AverageImportance { get; init; }

    public required DateTime DateFrom { get; init; }

    public required DateTime DateTo { get; init; }

    public required Dictionary<PromptTypeEnumViewModel, string> UsedPrompts { get; init; }
}

[NullChecks(false)]
public sealed record PostSummaryViewModel
{
    [Display(Name = "Feed Url")]
    public required string FeedTitle { get; init; }

    public required string Summary { get; init; }

    [Display(Name = "Original Link")]
    public required string Url { get; init; }

    [Display(Name = "Posted At")]
    public required DateTime PostedAt { get; init; }

    public required int Importance { get; init; }
}

public enum PromptTypeEnumViewModel
{
    [Display(Name = "Post Summary Prompt")]
    PostSummary,

    [Display(Name = "Post Importance Prompt")]
    PostImportance,

    [Display(Name = "Digest Summary Prompt")]
    DigestSummary,
}
