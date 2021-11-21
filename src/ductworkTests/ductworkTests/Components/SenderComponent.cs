using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Components;

#nullable enable
namespace ductworkTests.Components;

public class SenderComponent : Component
{
    public readonly OutputPlug<IObjectArtifact> Out = new();
        
    private readonly object[] _values;
        
    public SenderComponent(object[] values)
    {
        _values = values;
    }

    public SenderComponent(int[] values) : this(values.Cast<object>().ToArray())
    {
    }

    public override async Task Execute(Graph graph, CancellationToken token)
    {
        foreach (var value in _values)
        {
            var artifact = value switch
            {
                string stringValue => new StringArtifact(stringValue),
                int intValue => new IntArtifact(intValue),
                _ => new ObjectArtifact(value),
            };
            await graph.Push(Out, artifact);
        }
    }
}