using System;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Crates;
using ductwork.Executors;

#nullable enable
namespace ductwork.Components;

public abstract class Component : IComponent
{
    public string DisplayName { get; set; } = Guid.NewGuid().ToString();

    public abstract Task Execute(IExecutor executor, CancellationToken token);
}

public abstract class SingleInComponent : Component
{
    private const int InputWaitMs = 50;

    public readonly InputPlug In = new();

    public override async Task Execute(IExecutor executor, CancellationToken token)
    {
        var runner = executor.Runner;

        while (!executor.IsFinished(In))
        {
            token.ThrowIfCancellationRequested();

            if (executor.Count(In) == 0)
            {
                await Task.Delay(InputWaitMs, token);
                continue;
            }

            var crate = await executor.Get(In, token);
            await runner.RunAsync(() => ExecuteIn(executor, crate, token), token);
        }

        await runner.WaitAsync(token);
        await ExecuteComplete(executor, token);
    }

    protected abstract Task ExecuteIn(IExecutor executor, ICrate crate, CancellationToken token);

    protected virtual Task ExecuteComplete(IExecutor executor, CancellationToken token)
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