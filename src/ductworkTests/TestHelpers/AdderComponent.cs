using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Components;
using ductwork.Executors;

namespace ductworkTests.TestHelpers;

public record AdderComponent : Component
{
    public readonly InputPlug InX = new();
    public readonly InputPlug InY = new();
    public readonly OutputPlug Out = new();

    public override async Task Execute(IExecutor executor, CancellationToken token)
    {
        while (!executor.IsFinished(InX) && !executor.IsFinished(InY))
        {
            var xCrate = await executor.Get(InX, token);
            var yCrate = await executor.Get(InY, token);

            if (xCrate.Get<ObjectArtifact>() is not {Object: int xInt} ||
                yCrate.Get<ObjectArtifact>() is not {Object: int yInt})
            {
                continue;
            }

            var crate = executor.CreateCrate(new ObjectArtifact(xInt + yInt));
            await executor.Push(Out, crate);
        }
    }
}