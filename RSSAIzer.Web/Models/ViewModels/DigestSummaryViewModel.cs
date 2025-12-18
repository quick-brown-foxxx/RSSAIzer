using System.ComponentModel.DataAnnotations;
using RuntimeNullables;

namespace RSSAIzer.Web.Models.ViewModels;

[NullChecks(false)]
public sealed record DigestSummaryViewModel
{
    public required Guid Id { get; init; }

    [Display(Name = "Created At")]
    public required DateTime CreatedAt { get; init; }

    public required string Summary { get; init; }

    public required string Title { get; init; }

    public required int PostsCount { get; init; }
}
