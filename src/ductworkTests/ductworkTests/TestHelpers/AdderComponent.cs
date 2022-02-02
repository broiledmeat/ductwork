using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Components;
using ductwork.Executors;

#nullable enable
namespace ductworkTests.TestHelpers;

public class AdderComponent : Component
{
    public readonly InputPlug InX = new();
    public readonly InputPlug InY = new();
    public readonly OutputPlug Out = new();

    public override async Task Execute(IExecutor executor, CancellationToken token)
    {
        while (!executor.IsFinished(InX) && !executor.IsFinished(InY))
        {
            var xArtifact = await executor.Get(InX, token);
            var yArtifact = await executor.Get(InY, token);

            if (xArtifact is not ObjectArtifact {Object: int xInt} ||
                yArtifact is not ObjectArtifact {Object: int yInt})
            {
                continue;
            }

            var artifact = new ObjectArtifact(xInt + yInt);
            await executor.Push(Out, artifact);
        }
    }
}