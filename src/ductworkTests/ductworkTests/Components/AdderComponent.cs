using System.Threading;
using System.Threading.Tasks;
using ductwork;

namespace ductworkTests.Components
{
    public class AdderComponent : Component
    {
        public readonly InputPlug<int> InX = new();
        public readonly InputPlug<int> InY = new();
        public readonly OutputPlug<int> Out = new();

        public override async Task Execute(Graph graph, CancellationToken token)
        {
            while (!graph.IsFinished(InX) && !graph.IsFinished(InY))
            {
                var x = await graph.Get(InX, token);
                var y = await graph.Get(InY, token);
                await graph.Push(Out, x + y);
            }
        }
    }
}