using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Crates;
using ductwork.Executors;

namespace ductwork.Components;

public record InputAwaiterComponent : SingleInSingleOutComponent
{
    private readonly ConcurrentBag<ICrate> _crates = new();

    protected override Task ExecuteIn(IExecutor executor, ICrate crate, CancellationToken token)
    {
        _crates.Add(crate);
        return Task.CompletedTask;
    }

    protected override async Task ExecuteComplete(IExecutor executor, CancellationToken token)
    {
        await Task.WhenAll(_crates.Select(crate => executor.Push(Out, crate)));
        await base.ExecuteComplete(executor, token);
    }
}