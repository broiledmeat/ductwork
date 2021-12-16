using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;

namespace ductwork.Components;

public class InputAwaiterComponent : SingleInSingleOutComponent
{
    private readonly ConcurrentBag<IArtifact> _artifacts = new();

    protected override Task ExecuteIn(Graph graph, IArtifact value, CancellationToken token)
    {
        _artifacts.Add(value);
        return Task.CompletedTask;
    }

    protected override async Task ExecuteComplete(Graph graph, CancellationToken token)
    {
        await Task.WhenAll(_artifacts.Select(artifact => graph.Push(Out, artifact)));
        await base.ExecuteComplete(graph, token);
    }
}