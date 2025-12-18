using System.ComponentModel.DataAnnotations;
using RSSAIzer.Backend.Infrastructure;
using RSSAIzer.Backend.Options;
using RuntimeNullables;

namespace RSSAIzer.Backend.Db;

/// <summary>
/// Interface for entities owned by a user.
/// </summary>
internal interface IUserOwnedEntity
{
    /// <summary>
    /// The owning user's ID.
    /// </summary>
    Guid UserId { get; init; }

    /// <summary>
    /// Navigation property to the owning ApplicationUser.
    /// </summary>
    ApplicationUser? UserNav { get; init; }
}

/// <summary>
/// Represents a RSS feed
/// </summary>
[NullChecks(false)]
internal sealed class FeedEntity : IUserOwnedEntity
{
    /// <summary>
    /// RSS feed URL
    /// </summary>
    [MaxLength(2048)]
    public required string RssUrl { get; init; }

    [MaxLength(100)]
    public required string Title { get; init; }

    [MaxLength(1000)]
    public required string Description { get; init; }

    [MaxLength(2048)]
    public required string ImageUrl { get; init; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public required bool IsDeleted { get; set; }

    /// <inheritdoc/>
    public Guid UserId { get; init; }

    /// <inheritdoc/>
    public ApplicationUser? UserNav { get; init; }
}

/// <summary>
/// Digest that has multiple posts from different feeds
/// </summary>
[NullChecks(false)]
internal sealed class DigestEntity : IUserOwnedEntity
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Stores used prompts for the digest as JSON
    /// </summary>
    [MaxLength(4096)]
    [JsonString]
    public required string UsedPrompts { get; init; }

    /// <summary>
    /// Navigation property to DigestSummaryEntity
    /// </summary>
    public DigestSummaryEntity? SummaryNav { get; init; }

    /// <summary>
    /// Navigation property to PostSummaryEntities
    /// </summary>
    public ICollection<PostSummaryEntity>? PostsNav { get; init; }

    /// <inheritdoc/>
    public Guid UserId { get; init; }

    /// <inheritdoc/>
    public ApplicationUser? UserNav { get; init; }
}

/// <summary>
/// Metadata for a digest
/// </summary>
[NullChecks(false)]
internal sealed class DigestSummaryEntity
{
    /// <summary>
    /// Id of Summary but also Foreign Key to Digest
    /// </summary>
    public required Guid Id { get; init; }

    [MaxLength(200)]
    public required string Title { get; init; }

    [MaxLength(8192)]
    public required string PostsSummary { get; init; }

    public required int PostsCount { get; init; }

    public required double AverageImportance { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime DateFrom { get; init; }

    public required DateTime DateTo { get; init; }

    /// <summary>
    /// Navigation property to DigestEntity
    /// </summary>
    public DigestEntity? DigestNav { get; init; }
}

/// <summary>
/// AI generated summary of a post and post info
/// </summary>
[NullChecks(false)]
internal sealed class PostSummaryEntity : IUserOwnedEntity
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Foreign key to Feed
    /// </summary>
    [MaxLength(2048)]
    public required string FeedUrl { get; init; }

    [MaxLength(2000)]
    public required string Summary { get; init; }

    [MaxLength(2048)]
    public required string Url { get; init; }

    public required DateTime PublishedAt { get; init; }

    [Range(1, 10)]
    public required int Importance { get; init; }

    /// <summary>
    /// Foreign key to DigestEntity
    /// </summary>
    public required Guid DigestId { get; init; }

    /// <summary>
    /// Navigation property to FeedEntity
    /// </summary>
    public FeedEntity? FeedNav { get; init; }

    /// <summary>
    /// Navigation property to DigestEntity
    /// </summary>
    public DigestEntity? DigestNav { get; init; }

    /// <inheritdoc/>
    public Guid UserId { get; init; }

    /// <inheritdoc/>
    public ApplicationUser? UserNav { get; init; }
}

/// <summary>
/// Application inittings stored in the database (flat structure)
/// </summary>
[NullChecks(false)]
internal sealed class SettingsEntity : IUserOwnedEntity
{
    /// <summary>
    /// Primary key and foreign key to user
    /// </summary>
    [Key]
    public Guid UserId { get; init; }

    [MaxLength(200)]
    public required string EmailRecipient { get; init; }

    [MaxLength(32)]
    public required string DigestTimeUtc { get; init; }

    [MaxLength(200)]
    public required string SmtpSettingsHost { get; init; }

    public required int SmtpSettingsPort { get; init; }

    [MaxLength(200)]
    public required string SmtpSettingsUsername { get; init; }

    [MaxLength(200)]
    public required string SmtpSettingsPassword { get; init; }

    public required bool SmtpSettingsUseSsl { get; init; }

    [MaxLength(200)]
    public required string OpenAiSettingsApiKey { get; init; }

    [MaxLength(100)]
    public required string OpenAiSettingsModel { get; init; }

    [MaxLength(200)]
    public required string OpenAiSettingsEndpoint { get; init; }

    [MaxLength(8192)]
    public required string PromptSettingsPostSummaryUserPrompt { get; init; }

    [MaxLength(8192)]
    public required string PromptSettingsPostImportanceUserPrompt { get; init; }

    [MaxLength(8192)]
    public required string PromptSettingsDigestSummaryUserPrompt { get; init; }

    /// <inheritdoc/>
    public ApplicationUser? UserNav { get; init; }
}

/// <summary>
/// Enum for prompt types used in DigestEntity. Must match PromptTypeEnumModel.
/// </summary>
internal enum PromptTypeEnumEntity
{
    PostSummary,
    PostImportance,
    DigestSummary,
}
