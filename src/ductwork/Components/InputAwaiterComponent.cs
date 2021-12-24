using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Executors;

namespace ductwork.Components;

public class InputAwaiterComponent : SingleInSingleOutComponent
{
    private readonly ConcurrentBag<IArtifact> _artifacts = new();

    protected override Task ExecuteIn(GraphExecutor executor, IArtifact value, CancellationToken token)
    {
        _artifacts.Add(value);
        return Task.CompletedTask;
    }

    protected override async Task ExecuteComplete(GraphExecutor executor, CancellationToken token)
    {
        await Task.WhenAll(_artifacts.Select(artifact => executor.Push(Out, artifact)));
        await base.ExecuteComplete(executor, token);
    }
}