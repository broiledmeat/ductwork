using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Components;
using ductwork.Crates;
using ductwork.Executors;

namespace ductworkTests.TestHelpers;

public class ReceiverComponent : SingleInComponent
{
    private readonly object _lock = new();
    private readonly List<object> _values = new();

    public ReceiverComponent()
    {
        Values = new ReadOnlyCollection<object>(_values);
    }

    public readonly ReadOnlyCollection<object> Values;
        
    protected override Task ExecuteIn(IExecutor executor, ICrate crate, CancellationToken token)
    {
        if (crate.Get<ObjectArtifact>() is not { } objectArtifact)
        {
            return Task.CompletedTask;
        }
        
        lock (_lock)
        {
            _values.Add(objectArtifact.Object);
        }

        return Task.CompletedTask;
    }
}