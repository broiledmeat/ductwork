using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Components;

namespace ductworkTests.Components;

public class ArrayGenericComponent<T> : Component
{
    public ArrayGenericComponent(T[] values)
    {
    }

    public override Task Execute(Graph graph, CancellationToken token)
    {
        throw new System.NotImplementedException();
    }
}