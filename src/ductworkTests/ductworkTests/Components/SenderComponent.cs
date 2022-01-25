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

    public Setting<object[]> Values = new();

    public override async Task Execute(IExecutor executor, CancellationToken token)
    {
        foreach (var value in Values.Value)
        {
            var artifact = new ObjectArtifact(value);
            await executor.Push(Out, artifact);
        }
    }
}