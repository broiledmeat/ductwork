using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Components;

#nullable enable
namespace ductworkTests.Components;

public class AdderComponent : Component
{
    public readonly InputPlug InX = new();
    public readonly InputPlug InY = new();
    public readonly OutputPlug Out = new();

    public override async Task Execute(Graph graph, CancellationToken token)
    {
        while (!graph.IsFinished(InX) && !graph.IsFinished(InY))
        {
            var x = await graph.Get(InX, token);
            var y = await graph.Get(InY, token);

            if (x is not IntArtifact xInt || y is not IntArtifact yInt)
            {
                continue;
            }
            
            var artifact = new IntArtifact(xInt.Value + yInt.Value);
            await graph.Push(Out, artifact);
        }
    }
}