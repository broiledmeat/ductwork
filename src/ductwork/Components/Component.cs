using System;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;

#nullable enable
namespace ductwork.Components;

public abstract class Component
{
    public string DisplayName { get; set; } = Guid.NewGuid().ToString();

    public abstract Task Execute(Graph graph, CancellationToken token);

    public override string ToString()
    {
        const string removeSuffix = nameof(Component);
        var name = GetType().Name;
        name = name.EndsWith(removeSuffix) ? name[..^removeSuffix.Length] : name;
        return name;
    }
}

public abstract class SingleInComponent : Component
{
    private const int InputWaitMs = 50;

    public readonly InputPlug In = new();

    public override async Task Execute(Graph graph, CancellationToken token)
    {
        var runner = new TaskRunner();

        while (!graph.IsFinished(In))
        {
            token.ThrowIfCancellationRequested();

            if (graph.Count(In) == 0)
            {
                await Task.Delay(InputWaitMs, token);
                continue;
            }

            var value = await graph.Get(In, token);
            await runner.RunAsync(() => ExecuteIn(graph, value, token), token);
        }

        await runner.WaitAsync();
        await ExecuteComplete(graph, token);
    }

    protected abstract Task ExecuteIn(Graph graph, IArtifact artifact, CancellationToken token);

    protected virtual Task ExecuteComplete(Graph graph, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}

public abstract class SingleInSingleOutComponent : SingleInComponent
{
    public readonly OutputPlug Out = new();
}

public abstract class SingleOutComponent : Component
{
    public readonly OutputPlug Out = new();
}