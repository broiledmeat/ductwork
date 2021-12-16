using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;

#nullable enable
namespace ductworkTests.Components;

public class ReceiverComponent : SingleInComponent
{
    private readonly object _lock = new();
    private readonly List<object> _values = new();

    public ReceiverComponent()
    {
        Values = new ReadOnlyCollection<object>(_values);
    }

    public readonly ReadOnlyCollection<object> Values;
        
    protected override Task ExecuteIn(Graph graph, IArtifact artifact, CancellationToken token)
    {
        if (artifact is not IObjectArtifact objectArtifact)
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