using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RSSAIzer.Backend.Models;
using RSSAIzer.Backend.Options;

namespace RSSAIzer.Backend.Features.DigestParallelProcessing;

[UsedImplicitly]
internal sealed class DigestProcessor(
    ITaskTracker<DigestId> taskTracker,
    IServiceProvider serviceProvider,
    ILogger<DigestProcessor> logger,
    IOptions<BackendDeploymentOptions> deploymentOptions
) : BackgroundService
{
    private readonly SemaphoreSlim _semaphore = new(deploymentOptions.Value.MaxConcurrentAiTasks);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessSingleTask(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    $"Unhandled exception in {nameof(DigestProcessor)}, continuing to process tasks"
                );
            }
        }
    }

    private async Task ProcessSingleTask(CancellationToken lifecycleCt)
    {
        // Wait for a new task to be available and acquire a semaphore slot
        await _semaphore.WaitAsync(lifecycleCt);
        var (workItem, exceptionHandler, digestId) = await taskTracker.DequeueWaitingTask();

        // Run the task in the background without awaiting, allowing concurrency
        _ = Task.Run(
            async () =>
            {
                try
                {
                    // Move a task to in-progress and execute it
                    using var scope = serviceProvider.CreateScope();
                    var progressControlCt = taskTracker.MoveTaskToInProgress(digestId);
                    await workItem(
                        CancellationTokenSource
                            .CreateLinkedTokenSource(progressControlCt, lifecycleCt)
                            .Token,
                        scope
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error occurred during digest queue processing for DigestId: {DigestId}",
                        digestId
                    );
                    if (exceptionHandler != null)
                    {
                        await exceptionHandler(ex);
                    }
                }
                finally
                {
                    taskTracker.TryCompleteTaskInProgress(digestId);
                    _semaphore.Release();
                }
            },
            lifecycleCt
        );
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        taskTracker.CancelAllTasksInProgress();

        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        base.Dispose();

        taskTracker.Dispose();
        _semaphore.Dispose();
    }
}
