using System.ComponentModel.DataAnnotations;
using RuntimeNullables;

namespace RSSAIzer.Web.Models.ViewModels;

[NullChecks(false)]
public sealed record RssProvider
{
    public required string Name { get; init; }
    public required string BaseUrl { get; init; }
}

[NullChecks(false)]
public sealed record FeedViewModel
{
    [Display(Name = "Feed Title")]
    public required string Title { get; init; }

    [Display(Name = "Feed URL")]
    [Url]
    public required string Url { get; init; }
}

[NullChecks(false)]
[Display(Name = "Feed")]
public sealed record AddFeedViewModel
{
    [Display(Name = "Feed URL")]
    [Url]
    public required string FeedUrl { get; init; }
}
