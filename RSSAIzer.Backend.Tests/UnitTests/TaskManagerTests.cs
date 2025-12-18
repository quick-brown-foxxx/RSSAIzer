using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RSSAIzer.Backend.Features.DigestParallelProcessing;

namespace RSSAIzer.Backend.Tests.UnitTests;

[TestFixture]
[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
public sealed class TaskManagerTests
{
    private TaskManager<string> _taskManager;

    [SetUp]
    public void SetUp()
    {
        _taskManager = new();
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldAddTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);

        var waitingTasks = _taskManager.GetWaitingTasks();
        waitingTasks.Should().Contain(key);
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldThrowException_WhenTaskAlreadyInProgress()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.MoveTaskToInProgress(key);

        var action = () => _taskManager.AddTaskToWaitQueue(task, key);
        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task DequeueWaitingTask_ShouldReturnTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);

        var (dequeuedTask, _, dequeuedKey) = await _taskManager.DequeueWaitingTask();

        dequeuedTask.Should().Be(task);
        dequeuedKey.Should().Be(key);
    }

    [Test]
    public void MoveTaskToInProgress_ShouldMoveTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.MoveTaskToInProgress(key);

        var inProgressTasks = _taskManager.GetInProgressTasks();
        var waitingTasks = _taskManager.GetWaitingTasks();
        waitingTasks.Should().NotContain(key);
        inProgressTasks.Should().Contain(key);
    }

    [Test]
    public void CompleteTaskInProgress_ShouldRemoveTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.MoveTaskToInProgress(key);
        _taskManager.CompleteTaskInProgress(key);

        var inProgressTasks = _taskManager.GetInProgressTasks();
        inProgressTasks.Should().NotContain(key);
    }

    [Test]
    public void CancelTaskInProgress_ShouldCancelTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>(
            (ct, _) => Task.Delay(1000, ct)
        );
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);
        var cancellationToken = _taskManager.MoveTaskToInProgress(key);

        _taskManager.CancelTaskInProgress(key);

        cancellationToken.IsCancellationRequested.Should().BeTrue();
    }

    [Test]
    public void RemoveWaitingTask_ShouldRemoveTask()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.RemoveWaitingTask(key);

        var waitingTasks = _taskManager.GetWaitingTasks();
        waitingTasks.Should().NotContain(key);
    }

    [Test]
    public void RemoveWaitingTask_ShouldThrowException_WhenTaskNotFound()
    {
        var action = () => _taskManager.RemoveWaitingTask("nonexistent");
        action.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldThrow_WhenTaskAlreadyInWaitingQueue()
    {
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);

        _taskManager.AddTaskToWaitQueue(task, key);

        var action = () => _taskManager.AddTaskToWaitQueue(task, key);
        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task ConcurrentAddToWaitQueue_ThrowsForDuplicateKey()
    {
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var exceptions = new ConcurrentQueue<Exception>();

        var tasks = Enumerable
            .Range(0, 4)
            .Select(
                (_, _) =>
                    Task.Run(() =>
                    {
                        try
                        {
                            _taskManager.AddTaskToWaitQueue(task, key);
                        }
                        catch (InvalidOperationException ex)
                        {
                            exceptions.Enqueue(ex);
                        }
                    })
            );

        await Task.WhenAll(tasks);

        exceptions.Count.Should().Be(3); // Only 1 success, 3 failures
        _taskManager.GetWaitingTasks().Should().ContainSingle();
    }

    [Test]
    public async Task ConcurrentDequeue_ProcessesAllTasks()
    {
        var numThreads = 4;
        var numTasks = numThreads * 25;
        var addedKeys = new List<string>();

        for (var i = 0; i < numTasks; i++)
        {
            var key = $"task{i}";
            addedKeys.Add(key);
            _taskManager.AddTaskToWaitQueue((_, _) => Task.CompletedTask, key);
        }

        var processedKeys = new ConcurrentBag<string>();
        var dequeueTasks = Enumerable
            .Range(0, numThreads)
            .Select(
                (_, _) =>
                    Task.Run(async () =>
                    {
                        foreach (var __ in Enumerable.Range(0, numTasks / numThreads))
                        {
                            var (_, _, key) = await _taskManager.DequeueWaitingTask();
                            _taskManager.MoveTaskToInProgress(key);
                            _taskManager.CompleteTaskInProgress(key);
                            processedKeys.Add(key);
                        }
                    })
            )
            .ToList();

        // Allow time to process all tasks
        await Task.WhenAll(dequeueTasks);
        await Task.Delay(100); // Small delay for final processing

        processedKeys.Should().BeEquivalentTo(addedKeys);
    }

    [Test]
    public void StressTest_MultipleOperationsConcurrently()
    {
        var numTasks = 1000;
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
        };

        Parallel.For(
            0,
            numTasks,
            options,
            i =>
            {
                var key = $"task{i}";
                _taskManager.AddTaskToWaitQueue((_, _) => Task.CompletedTask, key);
                _taskManager.MoveTaskToInProgress(key);
                _taskManager.CompleteTaskInProgress(key);
            }
        );

        _taskManager.GetInProgressTasks().Should().BeEmpty();
        _taskManager.GetWaitingTasks().Should().BeEmpty();
    }

    [Test]
    public async Task CancelTaskInProgress_ShouldTerminateLongRunningTask()
    {
        var key = "task1";
        var taskCompleted = false;
        var cancellationConfirmed = false;

        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, ct);
                }
                catch (OperationCanceledException)
                {
                    cancellationConfirmed = true;
                    throw;
                }
                finally
                {
                    taskCompleted = true;
                }
            }
        );

        _taskManager.AddTaskToWaitQueue(task, key);
        var (taskToRun, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);
        _ = taskToRun(ct, Mock.Of<IServiceScope>());
        _taskManager.CancelTaskInProgress(key);

        await Task.Delay(200); // Allow cancellation to propagate

        taskCompleted.Should().BeTrue();
        cancellationConfirmed.Should().BeTrue();
    }

    [Test]
    public void CompleteTask_ShouldThrowForNonExistentKey()
    {
        var action = () => _taskManager.CompleteTaskInProgress("nonexistent");
        action.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void ConcurrentMoveToInProgress_ThrowsForDuplicateKey()
    {
        var key = "task1";
        _taskManager.AddTaskToWaitQueue((_, _) => Task.CompletedTask, key);

        var exceptions = new ConcurrentQueue<Exception>();

        var tasks = Enumerable
            .Range(0, 4)
            .Select(
                (_, _) =>
                    Task.Run(() =>
                    {
                        try
                        {
                            _taskManager.MoveTaskToInProgress(key);
                        }
                        catch (InvalidOperationException ex)
                        {
                            exceptions.Enqueue(ex);
                        }
                    })
            );

        Task.WaitAll(tasks.ToArray());

        exceptions.Count.Should().Be(3); // Only 1 success
        _taskManager.GetInProgressTasks().Should().ContainSingle();
    }

    [Test]
    public void AddTaskToWaitQueue_ShouldAllowReuseOfKey_AfterCompletion()
    {
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);
        var key = "task1";

        // Add, move to in-progress, complete.
        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.MoveTaskToInProgress(key);
        _taskManager.CompleteTaskInProgress(key);

        // Now the same key can be reused.
        var action = () => _taskManager.AddTaskToWaitQueue(task, key);
        action.Should().NotThrow();
        var waitingTasks = _taskManager.GetWaitingTasks();
        waitingTasks.Should().Contain(key);
    }

    [Test]
    public void MoveTaskToInProgress_ShouldThrow_WhenTaskNotInWaitingQueue()
    {
        var key = "nonexistent";
        var act = () => _taskManager.MoveTaskToInProgress(key);
        act.Should().Throw<InvalidOperationException>().WithMessage("*is not in waiting queue*");
    }

    [Test]
    public void CancelTaskInProgress_ShouldThrow_WhenTaskNotFound()
    {
        var key = "nonexistent";
        var action = () => _taskManager.CancelTaskInProgress(key);
        action
            .Should()
            .Throw<KeyNotFoundException>()
            .WithMessage("*was not found in progress tasks list*");
    }

    [Test]
    public void CancelTaskInProgress_ShouldNotThrow_WhenAlreadyCancelled()
    {
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>(
            (ct, _) => Task.Delay(1000, ct)
        );

        _taskManager.AddTaskToWaitQueue(task, key);
        var token = _taskManager.MoveTaskToInProgress(key);

        // First cancellation should cancel the token.
        var action = () => _taskManager.CancelTaskInProgress(key);
        action.Should().NotThrow();
        token.IsCancellationRequested.Should().BeTrue();

        // Calling cancellation again should not throw.
        action.Should().NotThrow();
    }

    [Test]
    public async Task DequeueWaitingTask_ShouldFollowFIFOOrder()
    {
        var keys = new[] { "first", "second", "third" };
        foreach (var key in keys)
        {
            _taskManager.AddTaskToWaitQueue((_, _) => Task.CompletedTask, key);
        }

        var dequeuedKeys = new List<string>();
        for (var i = 0; i < keys.Length; i++)
        {
            var (_, _, key) = await _taskManager.DequeueWaitingTask();
            dequeuedKeys.Add(key);
        }

        dequeuedKeys.Should().BeEquivalentTo(keys);
    }

    [Test]
    public void CompleteTaskInProgress_ShouldThrow_WhenCalledTwice()
    {
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);

        _taskManager.AddTaskToWaitQueue(task, key);
        _taskManager.MoveTaskToInProgress(key);
        _taskManager.CompleteTaskInProgress(key);

        var action = () => _taskManager.CompleteTaskInProgress(key);
        action.Should().Throw<KeyNotFoundException>().WithMessage("*is not in progress*");
    }

    [Test]
    public void GetCancellationRequestedTasks_ShouldReturnOnlyCancelledTasks()
    {
        // Arrange
        var runningKey = "running";
        var cancelledKey = "cancelled";
        var longRunningTask = new Func<CancellationToken, IServiceScope, Task>(
            (ct, _) => Task.Delay(TimeSpan.FromSeconds(10), ct)
        );

        _taskManager.AddTaskToWaitQueue(longRunningTask, runningKey);
        _taskManager.AddTaskToWaitQueue(longRunningTask, cancelledKey);

        _taskManager.MoveTaskToInProgress(runningKey);
        _taskManager.MoveTaskToInProgress(cancelledKey);

        // Act
        _taskManager.CancelTaskInProgress(cancelledKey);
        var cancelledTasks = _taskManager.GetCancellationRequestedTasks();

        // Assert
        cancelledTasks.Should().ContainSingle();
        cancelledTasks.Should().Contain(cancelledKey);
        cancelledTasks.Should().NotContain(runningKey);
    }

    [Test]
    public void GetCancellationRequestedTasks_ShouldReturnEmptyArray_WhenNoTasksCancelled()
    {
        // Arrange
        var key1 = "task1";
        var key2 = "task2";
        var task = new Func<CancellationToken, IServiceScope, Task>((_, _) => Task.CompletedTask);

        _taskManager.AddTaskToWaitQueue(task, key1);
        _taskManager.AddTaskToWaitQueue(task, key2);

        _taskManager.MoveTaskToInProgress(key1);
        _taskManager.MoveTaskToInProgress(key2);

        // Act
        var cancelledTasks = _taskManager.GetCancellationRequestedTasks();

        // Assert
        cancelledTasks.Should().BeEmpty();
    }

    [Test]
    public async Task CancelTask_ShouldExecuteCleanupCode()
    {
        // Arrange
        var key = "task1";
        var cleanupExecuted = false;
        var taskStarted = false;

        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
            {
                try
                {
                    taskStarted = true;
                    await Task.Delay(Timeout.Infinite, ct);
                }
                finally
                {
                    cleanupExecuted = true;
                }
            }
        );

        // Act
        _taskManager.AddTaskToWaitQueue(task, key);
        var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);

        // Start task without awaiting
        _ = dequeued(ct, Mock.Of<IServiceScope>());

        // Wait for a task to start
        await Task.Delay(100);
        taskStarted.Should().BeTrue("Task should have started");

        // Cancel and wait for completion
        _taskManager.CancelTaskInProgress(key);
        await Task.Delay(100);

        // Assert
        cleanupExecuted.Should().BeTrue("Cleanup code should have executed");
    }

    [Test]
    public async Task CancellationToken_ShouldPropagateToNestedOperations()
    {
        // Arrange
        var key = "task1";
        var innerTaskCancelled = false;
        var outerTaskCancelled = false;

        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (outerCt, _) =>
            {
                try
                {
                    await Task.Run(
                        async () =>
                        {
                            try
                            {
                                await Task.Delay(Timeout.Infinite, outerCt);
                            }
                            catch (OperationCanceledException)
                            {
                                innerTaskCancelled = true;
                                throw;
                            }
                        },
                        outerCt
                    );
                }
                catch (OperationCanceledException)
                {
                    outerTaskCancelled = true;
                    throw;
                }
            }
        );

        // Act
        _taskManager.AddTaskToWaitQueue(task, key);
        var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);

        _ = dequeued(ct, Mock.Of<IServiceScope>());
        await Task.Delay(100); // Give time for task to start

        _taskManager.CancelTaskInProgress(key);
        await Task.Delay(100); // Wait for cancellation to propagate

        // Assert
        innerTaskCancelled.Should().BeTrue("Inner task should be cancelled");
        outerTaskCancelled.Should().BeTrue("Outer task should be cancelled");
    }

    [Test]
    public async Task CancelledTask_ShouldAllowKeyReuse()
    {
        // Arrange
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
        );

        // Act - First use and cancellation
        _taskManager.AddTaskToWaitQueue(task, key);
        var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);

        var runningTask = dequeued(ct, Mock.Of<IServiceScope>());
        _taskManager.CancelTaskInProgress(key);

        try
        {
            await runningTask;
        }
        catch (OperationCanceledException) { }

        _taskManager.CompleteTaskInProgress(key);

        // Act - Reuse key
        var newTask = new Func<CancellationToken, IServiceScope, Task>(
            (_, _) => Task.CompletedTask
        );

        // Assert
        var action = async () =>
        {
            _taskManager.AddTaskToWaitQueue(newTask, key);
            await _taskManager.DequeueWaitingTask();
            _taskManager.MoveTaskToInProgress(key);
        };
        await action.Should().NotThrowAsync();
        _taskManager.GetInProgressTasks().Should().Contain(key);
    }

    [Test]
    public async Task CancelledTask_ShouldReceiveCancellation()
    {
        // Arrange
        var key = "task1";
        var cancellationReceived = false;

        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, ct);
                }
                catch (OperationCanceledException)
                {
                    cancellationReceived = true;
                    throw;
                }
            }
        );

        // Act
        _taskManager.AddTaskToWaitQueue(task, key);
        var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);

        var runningTask = dequeued(ct, Mock.Of<IServiceScope>());
        await Task.Delay(100); // Give task time to start

        _taskManager.CancelTaskInProgress(key);

        try
        {
            await runningTask;
        }
        catch (OperationCanceledException) { }

        // Assert
        cancellationReceived.Should().BeTrue("Task should have received cancellation");
    }

    [Test]
    public async Task CancelAllTasksInProgress_ShouldCancelAllRunningTasks()
    {
        // Arrange
        var taskCount = 3;
        var cancelledTasks = 0;

        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, ct);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancelledTasks);
                    throw;
                }
            }
        );

        // Add multiple tasks
        for (var i = 0; i < taskCount; i++)
        {
            var key = $"task{i}";
            _taskManager.AddTaskToWaitQueue(task, key);
            var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
            var ct = _taskManager.MoveTaskToInProgress(key);
            _ = dequeued(ct, Mock.Of<IServiceScope>());
        }

        await Task.Delay(100); // Allow tasks to start

        // Act
        _taskManager.CancelAllTasksInProgress();
        await Task.Delay(100); // Allow cancellation to propagate

        // Assert
        cancelledTasks.Should().Be(taskCount, "All tasks should have been cancelled");
        _taskManager.GetCancellationRequestedTasks().Should().HaveCount(taskCount);
    }

    [Test]
    public async Task CancelledTask_ShouldBeRemovedFromInProgressList()
    {
        // Arrange
        var key = "task1";
        var task = new Func<CancellationToken, IServiceScope, Task>(
            async (ct, _) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
        );

        // Act
        _taskManager.AddTaskToWaitQueue(task, key);
        var (dequeued, _, _) = await _taskManager.DequeueWaitingTask();
        var ct = _taskManager.MoveTaskToInProgress(key);

        var runningTask = dequeued(ct, Mock.Of<IServiceScope>());
        await Task.Delay(100); // Give task time to start

        _taskManager.CancelTaskInProgress(key);

        try
        {
            await runningTask;
        }
        catch (OperationCanceledException) { }

        _taskManager.CompleteTaskInProgress(key);

        // Assert
        _taskManager.GetInProgressTasks().Should().NotContain(key);
        _taskManager.GetCancellationRequestedTasks().Should().BeEmpty();
    }
}
