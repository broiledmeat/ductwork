using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ductwork
{
    public class TaskRunner : IDisposable
    {
        private static readonly SemaphoreSlim ExecutionSemaphore;
        private readonly ConcurrentBag<Task> _tasks = new();

        static TaskRunner()
        {
            var count = Math.Max(1, Environment.ProcessorCount - 1);
            ExecutionSemaphore = new SemaphoreSlim(count);
        }

        public async Task RunAsync(Func<Task> func, CancellationToken token)
        {
            await ExecutionSemaphore.WaitAsync(token);
            var task = Task.Run(() =>
                {
                    try
                    {
                        func().Wait(token);
                    }
                    finally
                    {
                        ExecutionSemaphore.Release();
                    }
                },
                token);
            _tasks.Add(task);
        }

        public async Task WaitAsync()
        {
            await Task.WhenAll(_tasks);
        }

        public void Dispose()
        {
            WaitAsync().Wait();
        }
    }
}