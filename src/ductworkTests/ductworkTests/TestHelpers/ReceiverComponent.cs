using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Executors;

#nullable enable
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
        
    protected override Task ExecuteIn(IExecutor executor, IArtifact artifact, CancellationToken token)
    {
        if (artifact is not ObjectArtifact objectArtifact)
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