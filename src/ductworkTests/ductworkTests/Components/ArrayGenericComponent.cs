using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Components;
using ductwork.Executors;

namespace ductworkTests.Components;

public class ArrayGenericComponent<T> : Component
{
    public ArrayGenericComponent(T[] values)
    {
    }

    public override Task Execute(GraphExecutor executor, CancellationToken token)
    {
        throw new System.NotImplementedException();
    }
}