using System.Threading.Channels;

namespace RSSAIzer.Backend.Features.DigestSteps;

/// <summary>
/// Singleton to share digest steps queue
/// </summary>
internal interface IDigestStepsChannel
{
    Channel<IDigestStepModel> Channel { get; }
}

internal sealed class DigestStepsChannel : IDigestStepsChannel
{
    public Channel<IDigestStepModel> Channel { get; } =
        System.Threading.Channels.Channel.CreateUnbounded<IDigestStepModel>(
            new() { SingleReader = true, SingleWriter = false }
        );
}
