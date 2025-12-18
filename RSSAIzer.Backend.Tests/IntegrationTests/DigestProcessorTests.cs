using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RSSAIzer.Backend.Features.DigestParallelProcessing;
using RSSAIzer.Backend.Models;
using RSSAIzer.Backend.Options;

namespace RSSAIzer.Backend.Tests.IntegrationTests;

[TestFixture]
[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
[SuppressMessage("Reliability", "CA2016:Forward the \'CancellationToken\' parameter to methods")]
public sealed class DigestProcessorTests
{
    private readonly Mock<ILogger<DigestProcessor>> _mockLogger = new();
    private readonly Mock<IServiceProvider> _mockServiceProvider = new();

    [SetUp]
    public void Setup()
    {
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(Mock.Of<IServiceScope>());
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
    }

    [Test]
    public async Task RealTracker_ProcessesTask_ThroughFullLifecycle()
    {
        // Arrange
        var tracker = new TaskManager<DigestId>();
        var service = new DigestProcessor(
            tracker,
            _mockServiceProvider.Object,
            _mockLogger.Object,
            Mock.Of<IOptions<BackendDeploymentOptions>>(x => x.Value.MaxConcurrentAiTasks == 2)
        );

        var digestId = new DigestId();
        var tcs = new TaskCompletionSource();

        // Add a task to queue
        tracker.AddTaskToWaitQueue((_, _) => tcs.Task, digestId);

        // Act
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        // Verify a task moved to in-progress
        await Task.Delay(100);
        tracker.GetInProgressTasks().Should().ContainSingle().And.Contain(digestId);

        // Complete task
        tcs.SetResult();
        await Task.Delay(100);

        // Assert
        tracker.GetInProgressTasks().Should().BeEmpty();

        // Cleanup
        cts.Dispose();
        service.Dispose();
    }

    [Test]
    public async Task RealTracker_RespectsConcurrencyLimit()
    {
        // Arrange
        var tracker = new TaskManager<DigestId>();
        var options = Mock.Of<IOptions<BackendDeploymentOptions>>(x =>
            x.Value.MaxConcurrentAiTasks == 2
        );
        var service = new DigestProcessor(
            tracker,
            _mockServiceProvider.Object,
            _mockLogger.Object,
            options
        );

        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();
        var tcs3 = new TaskCompletionSource();
        var digestId1 = new DigestId();
        var digestId2 = new DigestId();
        var digestId3 = new DigestId();

        // Add tasks
        tracker.AddTaskToWaitQueue((_, _) => tcs1.Task, digestId1);
        tracker.AddTaskToWaitQueue((_, _) => tcs2.Task, digestId2);
        tracker.AddTaskToWaitQueue((_, _) => tcs3.Task, digestId3);

        // Act
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        // Verify both tasks start immediately
        await Task.Delay(100);
        tracker.GetInProgressTasks().Should().HaveCount(2).And.Contain(digestId1, digestId2);
        tracker.GetWaitingTasks().Should().ContainSingle().And.Contain(digestId3);

        // Cleanup
        tcs1.SetResult();
        tcs2.SetResult();
        tcs3.SetResult();
        cts.Dispose();
        service.Dispose();
    }

    [Test]
    public async Task RealTracker_ExecuteAsync_HandlesCancellation_DuringProcessing()
    {
        // Arrange
        var tracker = new TaskManager<DigestId>();
        var service = new DigestProcessor(
            tracker,
            _mockServiceProvider.Object,
            _mockLogger.Object,
            Mock.Of<IOptions<BackendDeploymentOptions>>(x => x.Value.MaxConcurrentAiTasks == 1)
        );
        var cancellationCalled = false;

        var digestId = new DigestId();

        tracker.AddTaskToWaitQueue(
            (ct, _) =>
            {
                ct.Register(() => cancellationCalled = true);
                return Task.CompletedTask;
            },
            digestId
        );

        // Act
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await cts.CancelAsync(); // Cancel service

        await Task.Delay(100); // Let the task complete

        // Assert
        cancellationCalled.Should().BeTrue();

        // Cleanup
        service.Dispose();
    }

    [Test]
    public async Task ProcessSingleTask_WhenExceptionHandlerThrows_LogsErrorAndContinues()
    {
        // Arrange
        var tracker = new TaskManager<DigestId>();
        var service = new DigestProcessor(
            tracker,
            _mockServiceProvider.Object,
            _mockLogger.Object,
            Mock.Of<IOptions<BackendDeploymentOptions>>(x => x.Value.MaxConcurrentAiTasks == 1)
        );

        var digestId = new DigestId();
        var workItemExecuted = false;
        var handlerExecuted = false;

        tracker.AddTaskToWaitQueue(
            (_, _) =>
            {
                workItemExecuted = true;
                throw new("Work item exception");
            },
            digestId,
            _ =>
            {
                handlerExecuted = true;
                throw new("Handler exception");
            }
        );

        // Act
        var cts = new CancellationTokenSource();
        _ = service.StartAsync(cts.Token);
        await Task.Delay(1000);

        // Assert
        workItemExecuted.Should().BeTrue("Work item should execute");
        handlerExecuted.Should().BeTrue("Exception handler should execute");

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.Is<Exception>(e => e.Message == "Work item exception"),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
                ),
            Times.Once
        );

        // Cleanup
        _ = service.StopAsync(CancellationToken.None);
        await Task.Delay(1000);
        service.Dispose();
    }

    [Test]
    public async Task StopAsync_WhenTasksAreInProgress_WaitsForCompletionAndCancelsThem()
    {
        // Arrange
        var tracker = new TaskManager<DigestId>();
        var service = new DigestProcessor(
            tracker,
            _mockServiceProvider.Object,
            _mockLogger.Object,
            Mock.Of<IOptions<BackendDeploymentOptions>>(x => x.Value.MaxConcurrentAiTasks == 2)
        );

        var tcs = new TaskCompletionSource();
        var digestId = new DigestId();
        var taskCancelled = false;

        tracker.AddTaskToWaitQueue(
            (ct, _) =>
            {
                ct.Register(() => taskCancelled = true);
                return tcs.Task;
            },
            digestId
        );

        // Start service and let the task begin execution
        _ = service.StartAsync(CancellationToken.None);
        await Task.Delay(1000);

        // Act
        _ = service.StopAsync(CancellationToken.None);

        // Complete the running task
        tcs.SetResult();
        await Task.Delay(1000);

        // Assert
        taskCancelled.Should().BeTrue("Task should be cancelled during shutdown");
        tracker.GetInProgressTasks().Should().BeEmpty("No tasks should remain in progress");

        service.Dispose();
    }

    [Test]
    public async Task ProcessSingleTask_WhenLifecycleCancellationRequested_CancelsRunningTasks()
    {
        // Arrange
        var tracker = new TaskManager<DigestId>();
        var service = new DigestProcessor(
            tracker,
            _mockServiceProvider.Object,
            _mockLogger.Object,
            Mock.Of<IOptions<BackendDeploymentOptions>>(x => x.Value.MaxConcurrentAiTasks == 1)
        );

        var digestId = new DigestId();
        var taskStarted = new TaskCompletionSource();
        var taskCancelled = false;

        tracker.AddTaskToWaitQueue(
            async (ct, _) =>
            {
                ct.Register(() => taskCancelled = true);
                taskStarted.SetResult();
                // Simulate long-running task
                await Task.Delay(Timeout.Infinite, ct);
            },
            digestId
        );

        // Act
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        // Wait for task to start
        await taskStarted.Task;

        // Cancel the service
        await cts.CancelAsync();
        await Task.Delay(100);

        // Assert
        taskCancelled.Should().BeTrue("Task should be cancelled");
        tracker.GetInProgressTasks().Should().BeEmpty("No tasks should remain in progress");

        service.Dispose();
    }
}
