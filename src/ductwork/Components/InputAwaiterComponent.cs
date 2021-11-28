using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;

namespace ductwork.Components;

public class InputAwaiterComponent<T> : SingleInSingleOutComponent<T, T> where T : IArtifact
{
    private readonly ConcurrentBag<T> _artifacts = new();

    protected override Task ExecuteIn(Graph graph, T value, CancellationToken token)
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