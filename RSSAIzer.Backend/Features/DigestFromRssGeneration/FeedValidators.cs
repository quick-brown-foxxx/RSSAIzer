using System.ServiceModel.Syndication;
using FluentValidation;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Features.DigestFromRssGeneration;

internal enum FeedFormat
{
    Unknown,
    Atom,
    Rss20,
}

internal sealed class SyndicationFeedValidator : AbstractValidator<SyndicationFeed>
{
    public SyndicationFeedValidator()
    {
        // Ensure has Title
        RuleFor(x => x.Title)
            .NotNull()
            .WithMessage("Feed title cannot be null")
            .Must(title => !string.IsNullOrWhiteSpace(title?.Text))
            .WithMessage("Feed title cannot be null or empty");
    }
}

internal sealed class AtomItemValidator : AbstractValidator<SyndicationItem>
{
    public AtomItemValidator()
    {
        // Atom feeds use Content, not Summary
        RuleFor(x => x.Content)
            .NotNull()
            .WithMessage("Atom feed item content cannot be null")
            .Must(content =>
            {
                if (content == null)
                    return false;
                // Content can be TextSyndicationContent, XmlSyndicationContent, etc.
                // For Atom feeds, we expect TextSyndicationContent
                if (content is TextSyndicationContent textContent)
                {
                    return !string.IsNullOrWhiteSpace(textContent.Text);
                }
                // For other content types, check if they have any readable content
                return true; // Allow other content types for now
            })
            .WithMessage("Atom feed item content text cannot be null or empty");

        RuleFor(x => x.Links)
            .NotNull()
            .WithMessage("Feed item links collection cannot be null")
            .Must(links => links != null && links.Any(l => l.RelationshipType == "alternate"))
            .WithMessage("Feed item must have at least one alternate link");

        RuleFor(x => x.Links)
            .Must(links =>
            {
                if (links == null)
                    return false;
                var alternateLink = links.FirstOrDefault(l => l.RelationshipType == "alternate");
                return alternateLink != null
                    && alternateLink.Uri != null
                    && alternateLink.Uri.IsAbsoluteUri;
            })
            .WithMessage("Feed item alternate link must be a valid absolute URI");

        RuleFor(x => x.PublishDate)
            .Must(date => date.DateTime <= DateTime.UtcNow.AddDays(1))
            .WithMessage("Feed item published date cannot be more than 1 day in the future")
            .Must(date => date.DateTime >= DateTime.UtcNow.AddYears(-50))
            .WithMessage("Feed item published date cannot be more than 50 years in the past");
    }
}

internal sealed class RssItemValidator : AbstractValidator<SyndicationItem>
{
    public RssItemValidator()
    {
        // RSS feeds use Summary (from description element), not Content
        RuleFor(x => x.Summary)
            .NotNull()
            .WithMessage("RSS feed item summary cannot be null")
            .Must(summary => !string.IsNullOrWhiteSpace(summary?.Text))
            .WithMessage("RSS feed item summary text cannot be null or empty");

        RuleFor(x => x.Links)
            .NotNull()
            .WithMessage("Feed item links collection cannot be null")
            .Must(links => links != null && links.Any(l => l.RelationshipType == "alternate"))
            .WithMessage("Feed item must have at least one alternate link");

        RuleFor(x => x.Links)
            .Must(links =>
            {
                if (links == null)
                    return false;
                var alternateLink = links.FirstOrDefault(l => l.RelationshipType == "alternate");
                return alternateLink != null
                    && alternateLink.Uri != null
                    && alternateLink.Uri.IsAbsoluteUri;
            })
            .WithMessage("Feed item alternate link must be a valid absolute URI");

        RuleFor(x => x.PublishDate)
            .Must(date => date.DateTime <= DateTime.UtcNow.AddDays(1))
            .WithMessage("Feed item published date cannot be more than 1 day in the future")
            .Must(date => date.DateTime >= DateTime.UtcNow.AddYears(-50))
            .WithMessage("Feed item published date cannot be more than 50 years in the past");
    }
}
