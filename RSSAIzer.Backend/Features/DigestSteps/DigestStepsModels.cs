using FluentResults;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Features.DigestSteps;

public enum DigestStepTypeModelEnum
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

public interface IDigestStepModel
{
    public DigestId DigestId { get; }
    public DigestStepTypeModelEnum Type { get; }
    public string? Message { get; }
    public DateTime Timestamp { get; }
}

internal sealed record SimpleStepModel : IDigestStepModel
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required DigestId DigestId { get; init; }
    public required DigestStepTypeModelEnum Type { get; init; }
    public string? Message { get; init; }
}

public sealed record AiProcessingStepModel : IDigestStepModel
{
    public AiProcessingStepModel()
    {
        if (Percentage is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(Percentage));
        }
    }

    public DigestStepTypeModelEnum Type => DigestStepTypeModelEnum.AiProcessing;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required DigestId DigestId { get; init; }
    public required int Percentage { get; init; }
    public string? Message { get; init; }
}

public sealed record RssReadingStartedStepModel : IDigestStepModel
{
    public DigestStepTypeModelEnum Type => DigestStepTypeModelEnum.RssReadingStarted;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required DigestId DigestId { get; init; }
    public required FeedUrl[] Feeds { get; init; }
    public string? Message { get; init; }
}

public sealed record RssReadingFinishedStepModel : IDigestStepModel
{
    public DigestStepTypeModelEnum Type => DigestStepTypeModelEnum.RssReadingFinished;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required DigestId DigestId { get; init; }
    public required int PostsCount { get; init; }
    public string? Message { get; init; }
}

public sealed record ErrorStepModel : IDigestStepModel
{
    public DigestStepTypeModelEnum Type => DigestStepTypeModelEnum.Error;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required DigestId DigestId { get; init; }
    public Exception? Exception { get; init; }
    public List<IError>? Errors { get; init; }
    public string? Message { get; init; }
}
