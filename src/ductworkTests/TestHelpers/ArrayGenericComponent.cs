using System;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Components;
using ductwork.Executors;

namespace ductworkTests.TestHelpers;

public class ArrayGenericComponent<T> : Component
{
    public Setting<T[]> Values;

    public override Task Execute(IExecutor executor, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}