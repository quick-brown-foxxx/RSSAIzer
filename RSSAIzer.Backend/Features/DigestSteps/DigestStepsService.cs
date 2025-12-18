using FluentResults;
using RSSAIzer.Backend.Db;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Features.DigestSteps;

internal interface IDigestStepsService
{
    void AddStep(IDigestStepModel step);
    Task<Result<IDigestStepModel[]>> GetAllSteps(DigestId digestId, CancellationToken ct);
}

internal sealed class DigestStepsService(
    IDigestStepsRepository repository,
    IDigestStepsChannel digestStepsChannel,
    ILogger<DigestStepsService> logger
) : IDigestStepsService
{
    public async Task<Result<IDigestStepModel[]>> GetAllSteps(
        DigestId digestId,
        CancellationToken ct
    )
    {
        return await repository.LoadStepsHistory(digestId, ct);
    }

    public void AddStep(IDigestStepModel step)
    {
        try
        {
            if (!digestStepsChannel.Channel.Writer.TryWrite(step))
            {
                logger.LogWarning(
                    "Failed to add step to channel for digest {DigestId}",
                    step.DigestId
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error while trying to add step for digest {DigestId}",
                step.DigestId
            );
        }
    }
}
