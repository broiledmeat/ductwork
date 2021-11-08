using System.Threading;
using System.Threading.Tasks;
using ductwork;

#nullable enable
namespace ductworkTests.Components
{
    public class SenderComponent<T> : Component
    {
        public readonly OutputPlug<T> Out = new();
        
        private readonly T[] _values;
        
        public SenderComponent(T[] values)
        {
            _values = values;
        }

        public override async Task Execute(Graph graph, CancellationToken token)
        {
            foreach (var value in _values)
            {
                await graph.Push(Out, value);
            }
        }
    }
}