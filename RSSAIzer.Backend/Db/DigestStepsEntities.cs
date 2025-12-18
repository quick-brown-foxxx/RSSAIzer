using System.ComponentModel.DataAnnotations;
using RuntimeNullables;

namespace RSSAIzer.Backend.Db;

internal enum DigestStepTypeEntityEnum
{
    Queued,
    ProcessingStarted,
    RssReadingStarted,
    RssReadingFinished,
    AiProcessing,
    Success,
    Cancelled,
    Error,
    NoPostsFound,
}

[NullChecks(false)]
internal abstract record DigestStepEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required Guid DigestId { get; init; }

    public required DigestStepTypeEntityEnum Type { get; init; }

    [MaxLength(2048)]
    public required string? Message { get; init; }

    public required DateTime Timestamp { get; init; }
}

internal sealed record SimpleStepEntity : DigestStepEntity { }

[NullChecks(false)]
internal sealed record AiProcessingStepEntity : DigestStepEntity
{
    [Range(0, 100)]
    public required int Percentage { get; init; }
}

[NullChecks(false)]
internal sealed record RssReadingStartedStepEntity : DigestStepEntity
{
    [Required]
    public required string? FeedsJson { get; init; }
}

[NullChecks(false)]
internal sealed record RssReadingFinishedStepEntity : DigestStepEntity
{
    public required int PostsFound { get; init; }
}

[NullChecks(false)]
internal sealed record ErrorStepEntity : DigestStepEntity
{
    public required string? ExceptionJsonSerialized { get; init; }
    public required string? ErrorsJsonSerialized { get; init; }
}
