using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Components;
using ductwork.Executors;

#nullable enable
namespace ductworkTests.Components;

public class AdderComponent : Component
{
    public readonly InputPlug InX = new();
    public readonly InputPlug InY = new();
    public readonly OutputPlug Out = new();

    public override async Task Execute(GraphExecutor executor, CancellationToken token)
    {
        while (!executor.IsFinished(InX) && !executor.IsFinished(InY))
        {
            var x = await executor.Get(InX, token);
            var y = await executor.Get(InY, token);

            if (x is not IntArtifact xInt || y is not IntArtifact yInt)
            {
                continue;
            }
            
            var artifact = new IntArtifact(xInt.Value + yInt.Value);
            await executor.Push(Out, artifact);
        }
    }
}