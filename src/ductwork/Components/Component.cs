using System;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Crates;
using ductwork.Executors;

namespace ductwork.Components;

public abstract record Component : IComponent
{
    public string DisplayName { get; set; } = Guid.NewGuid().ToString();

    public abstract Task Execute(IExecutor executor, CancellationToken token);
}

public abstract record SingleInComponent : Component
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

public abstract record SingleInSingleOutComponent : SingleInComponent
{
    public readonly OutputPlug Out = new();
}

public abstract record SingleOutComponent : Component
{
    public readonly OutputPlug Out = new();
}