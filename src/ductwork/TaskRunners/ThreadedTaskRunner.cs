using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork.TaskRunners;

/// <summary>
/// Task pool that limits the number of tasks running in parallel. 
/// </summary>
public class ThreadedTaskRunner : TaskRunner
{
    private readonly SemaphoreSlim _executionSemaphore;
    private readonly ConcurrentBag<Task> _tasks = new();
    
    public ThreadedTaskRunner(int maximum = -1)
    {
        var maximumParallelTasks = maximum > 0 ? maximum : Math.Max(1, Environment.ProcessorCount - 1);
        _executionSemaphore = new SemaphoreSlim(maximumParallelTasks, maximumParallelTasks);
    }

    /// <summary>
    /// Run the given <paramref name="func"/> asynchronously. If the number of running tasks is equal or greater to
    /// `max(1, num_processors - 1))`, the runner will wait for a task to complete before executing the given
    /// <paramref name="func"/>.
    /// </summary>
    /// <param name="func">The function to execute asynchronously.</param>
    /// <param name="token">Cancellation token.</param>
    public override async Task RunAsync(Func<Task> func, CancellationToken token)
    {
        await _executionSemaphore.WaitAsync(token);
        var task = Task.Run(() =>
            {
                try
                {
                    func().Wait(token);
                }
                finally
                {
                    _executionSemaphore.Release();
                }
            },
            token);
        _tasks.Add(task);
    }

    /// <summary>
    /// Asynchronously await for all current tasks to complete.
    /// </summary>
    public override async Task WaitAsync(CancellationToken token)
    {
        await Task.WhenAll(_tasks);
    }

    public override void Dispose()
    {
        base.Dispose();
        WaitAsync(CancellationToken.None).Wait();
    }
}