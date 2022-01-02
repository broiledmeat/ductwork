using System.Threading;
using System.Threading.Tasks;
using ductwork.Components;
using ductwork.Executors;

#nullable enable
namespace ductworkTests.Components;

public class ArrayGenericComponent<T> : Component
{
    public Setting<T[]> Values;

    public override Task Execute(GraphExecutor executor, CancellationToken token)
    {
        throw new System.NotImplementedException();
    }
}