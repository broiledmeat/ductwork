using System;
using System.Threading;
using System.Threading.Tasks;

namespace ductwork.TaskRunners;

public abstract class TaskRunner : IDisposable
{
    public abstract Task RunAsync(Func<Task> func, CancellationToken token);

    public abstract Task WaitAsync(CancellationToken token);
    
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}