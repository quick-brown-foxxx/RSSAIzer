using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RSSAIzer.Backend.Db;

namespace RSSAIzer.Backend.Features.DigestSteps;

internal sealed class DigestStepsProcessor(
    IServiceScopeFactory scopeFactory,
    IDigestStepsChannel digestStepsChannel,
    ILogger<DigestStepsProcessor> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IDigestStepModel? step = null;
            try
            {
                step = await digestStepsChannel.Channel.Reader.ReadAsync(stoppingToken);
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IDigestStepsRepository>();
                await repository.SaveStepAsync(step, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation(
                    "Cancellation requested, Stopping new digest steps processing, "
                );
                break;
            }
            catch (Exception ex)
            {
                if (step != null)
                {
                    logger.LogError(
                        ex,
                        "Failed to save step {Type} for digest {DigestId} with message [{Message}]",
                        step.Type,
                        step.DigestId,
                        step.Message
                    );
                }

                logger.LogError(
                    ex,
                    "Unhandled error while processing steps, stopping new DigestSteps processing"
                );
                break;
            }
        }
    }
}
