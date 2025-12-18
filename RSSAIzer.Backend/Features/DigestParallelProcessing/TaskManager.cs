using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace RSSAIzer.Backend.Features.DigestParallelProcessing;

internal interface ITaskScheduler<TKey> : IDisposable
    where TKey : IEquatable<TKey>
{
    /// <exception cref="InvalidOperationException">
    /// Thrown if the task is already in progress or in the waiting queue.
    /// </exception>
    void AddTaskToWaitQueue(
        Func<CancellationToken, IServiceScope, Task> task,
        TKey key,
        Func<Exception, Task>? exceptionHandler = null
    );

    /// <exception cref="KeyNotFoundException">
    /// Thrown if the key is not found in the waiting tasks list.
    /// </exception>
    void RemoveWaitingTask(TKey key);

    /// <returns>A collection of keys.</returns>
    TKey[] GetWaitingTasks();

    /// <returns>A collection of keys.</returns>
    TKey[] GetInProgressTasks();

    /// <returns>A collection of keys.</returns>
    TKey[] GetCancellationRequestedTasks();

    /// <param name="key">The key associated with the task to be canceled.</param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the key is not found in the in-progress tasks list.
    /// </exception>
    void CancelTaskInProgress(TKey key);
}

internal interface ITaskTracker<TKey> : IDisposable
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Waits for a task to be available in the queue and returns it.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the task and its key.</returns>
    public Task<(
        Func<CancellationToken, IServiceScope, Task> taskFactory,
        Func<Exception, Task>? exceptionHandler,
        TKey key
    )> DequeueWaitingTask();

    /// <summary>
    /// Moves a task from the waiting queue to in-progress state.
    /// </summary>
    /// <returns>A cancellation token that can be used to cancel the task in progress.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the task is not in the waiting queue or is already in progress.
    /// </exception>
    public CancellationToken MoveTaskToInProgress(TKey key);

    /// <summary>
    /// Marks a task as complete and removes it from the in-progress state.
    /// </summary>
    /// <param name="key">The key associated with the task to be completed.</param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the key is not found in the in-progress tasks list.
    /// </exception>
    public void CompleteTaskInProgress(TKey key);

    /// <summary>
    /// Tries to mark a task as complete and removes it from the in-progress state.
    /// </summary>
    /// <param name="key">The key associated with the task to be completed.</param>
    /// <returns>True if the task was successfully completed, false otherwise</returns>
    public bool TryCompleteTaskInProgress(TKey key);

    /// <summary>
    /// Cancels all tasks in progress.
    /// </summary>
    public void CancelAllTasksInProgress();
}

internal sealed class TaskManager<TKey> : ITaskScheduler<TKey>, ITaskTracker<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly Channel<(
        Func<CancellationToken, IServiceScope, Task> taskFactory,
        Func<Exception, Task>? exceptionHandler,
        TKey key
    )> _waitingTasksQueue = Channel.CreateUnbounded<(
        Func<CancellationToken, IServiceScope, Task> taskFactory,
        Func<Exception, Task>? exceptionHandler,
        TKey key
    )>();
    private readonly ConcurrentDictionary<
        TKey,
        Func<CancellationToken, IServiceScope, Task>
    > _waitingTasksList = new();
    private readonly ConcurrentDictionary<TKey, CancellationTokenSource> _inProgressTasksCts =
        new();

    public void AddTaskToWaitQueue(
        Func<CancellationToken, IServiceScope, Task> task,
        TKey key,
        Func<Exception, Task>? exceptionHandler = null
    )
    {
        if (_inProgressTasksCts.ContainsKey(key))
        {
            throw new InvalidOperationException($"Task {key} is already in progress");
        }

        if (!_waitingTasksList.TryAdd(key, task))
        {
            throw new InvalidOperationException($"Task {key} is already in waiting queue");
        }

        if (!_waitingTasksQueue.Writer.TryWrite((task, exceptionHandler, key)))
        {
            _waitingTasksList.TryRemove(key, out _);
            throw new InvalidOperationException("Failed to write to the background task queue.");
        }
    }

    public async Task<(
        Func<CancellationToken, IServiceScope, Task> taskFactory,
        Func<Exception, Task>? exceptionHandler,
        TKey key
    )> DequeueWaitingTask()
    {
        var item = await _waitingTasksQueue.Reader.ReadAsync();
        return item;
    }

    public CancellationToken MoveTaskToInProgress(TKey key)
    {
        if (!_waitingTasksList.TryRemove(key, out _))
        {
            throw new InvalidOperationException($"Task {key} is not in waiting queue");
        }

        var cts = new CancellationTokenSource();
        if (!_inProgressTasksCts.TryAdd(key, cts))
        {
            cts.Dispose();
            throw new InvalidOperationException($"Task {key} is already in progress");
        }

        return cts.Token;
    }

    public TKey[] GetInProgressTasks()
    {
        return _inProgressTasksCts.Keys.ToArray();
    }

    public TKey[] GetCancellationRequestedTasks()
    {
        return _inProgressTasksCts
            .Where(x => x.Value.IsCancellationRequested)
            .Select(x => x.Key)
            .ToArray();
    }

    public void CompleteTaskInProgress(TKey key)
    {
        if (!_inProgressTasksCts.TryRemove(key, out var cts))
        {
            throw new KeyNotFoundException($"Task {key} is not in progress");
        }

        cts.Dispose();
    }

    public bool TryCompleteTaskInProgress(TKey key)
    {
        try
        {
            CompleteTaskInProgress(key);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    public TKey[] GetWaitingTasks()
    {
        return _waitingTasksList.Keys.ToArray();
    }

    public void RemoveWaitingTask(TKey key)
    {
        if (!_waitingTasksList.TryRemove(key, out _))
        {
            throw new KeyNotFoundException($"The key {key} was not found in waiting tasks list.");
        }
    }

    public void CancelTaskInProgress(TKey key)
    {
        if (!_inProgressTasksCts.TryGetValue(key, out var cts))
        {
            throw new KeyNotFoundException($"The key {key} was not found in progress tasks list");
        }

        cts.Cancel();
    }

    public void CancelAllTasksInProgress()
    {
        foreach (var cts in _inProgressTasksCts.Values)
        {
            cts.Cancel();
        }
    }

    public void Dispose()
    {
        foreach (var cts in _inProgressTasksCts.Values)
        {
            cts.Dispose();
        }

        _waitingTasksQueue.Writer.TryComplete();
        _waitingTasksQueue.Reader.Completion.Wait();
    }
}
