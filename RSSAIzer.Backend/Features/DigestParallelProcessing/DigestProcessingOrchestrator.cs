using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using RSSAIzer.Backend.Features.DigestFromRssGeneration;
using RSSAIzer.Backend.Features.DigestSteps;
using RSSAIzer.Backend.Models;

namespace RSSAIzer.Backend.Features.DigestParallelProcessing;

internal interface IDigestProcessingOrchestrator
{
    Task<Result<DigestGenerationResultModelEnum>> ProcessDigest(
        DigestId digestId,
        DigestParametersModel parameters,
        CancellationToken ct
    );

    void QueueDigest(DigestId digestId, DigestParametersModel parameters, CancellationToken ct);
}

internal sealed class DigestProcessingOrchestrator(
    IDigestService digestService,
    IDigestStepsService digestStepsService,
    ITaskScheduler<DigestId> taskScheduler,
    ILogger<DigestProcessingOrchestrator> logger
) : IDigestProcessingOrchestrator
{
    public async Task<Result<DigestGenerationResultModelEnum>> ProcessDigest(
        DigestId digestId,
        DigestParametersModel parameters,
        CancellationToken ct
    )
    {
        digestStepsService.AddStep(
            new SimpleStepModel
            {
                DigestId = digestId,
                Type = DigestStepTypeModelEnum.ProcessingStarted,
            }
        );

        var generationResult = await digestService.GenerateDigest(digestId, parameters, ct);
        if (generationResult.IsFailed)
        {
            logger.LogError(
                "Failed to generate digest: {Errors}",
                string.Join(", ", generationResult.Errors)
            );
            return Result.Fail(generationResult.Errors);
        }

        logger.LogInformation("Digest generation completed successfully");
        return Result.Ok(generationResult.Value);
    }

    public void QueueDigest(
        DigestId digestId,
        DigestParametersModel parameters,
        CancellationToken ct
    )
    {
        digestStepsService.AddStep(
            new SimpleStepModel { DigestId = digestId, Type = DigestStepTypeModelEnum.Queued }
        );

        taskScheduler.AddTaskToWaitQueue(
            async (localCt, scope) =>
            {
                // use own scope and services to avoid issues with disposing of captured scope
                var mainService = scope.ServiceProvider.GetRequiredService<IMainService>();
                var mergedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, localCt).Token;
                await mainService.ProcessDigest(digestId, parameters, mergedCt);
            },
            digestId,
            ex =>
            {
                if (ex is OperationCanceledException)
                {
                    logger.LogInformation("Digest {DigestId} processing was canceled", digestId);
                    digestStepsService.AddStep(
                        new SimpleStepModel
                        {
                            DigestId = digestId,
                            Type = DigestStepTypeModelEnum.Cancelled,
                        }
                    );
                }
                else
                {
                    logger.LogError(
                        ex,
                        "Unhandled exception while trying to process digest {DigestId}",
                        digestId
                    );
                    digestStepsService.AddStep(
                        new ErrorStepModel
                        {
                            DigestId = digestId,
                            Exception = ex,
                            Message = "Unhandled exception while trying to process digest",
                        }
                    );
                }

                return Task.CompletedTask;
            }
        );
    }
}
