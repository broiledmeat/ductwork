using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Executors;

#nullable enable
namespace ductworkTests.Components;

public class SenderComponent : Component
{
    public readonly OutputPlug Out = new();
        
    private readonly object[] _values;
        
    public SenderComponent(object[] values)
    {
        _values = values;
    }

    public SenderComponent(int[] values) : this(values.Cast<object>().ToArray())
    {
    }

    public override async Task Execute(GraphExecutor graph, CancellationToken token)
    {
        foreach (var value in _values)
        {
            var artifact = (IArtifact)(value switch
            {
                string stringValue => new StringArtifact(stringValue),
                int intValue => new IntArtifact(intValue),
                _ => new ObjectArtifact(value),
            });
            await graph.Push(Out, artifact);
        }
    }
}