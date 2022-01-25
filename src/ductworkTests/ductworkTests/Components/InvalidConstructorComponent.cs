using System.Threading;
using System.Threading.Tasks;
using ductwork.Components;
using ductwork.Executors;

#nullable enable
namespace ductworkTests.Components;

public class InvalidConstructorComponent : Component
{
    public InvalidConstructorComponent(bool unused)
    {
    }

    public override Task Execute(IExecutor executor, CancellationToken token)
    {
        throw new System.NotImplementedException();
    }
}