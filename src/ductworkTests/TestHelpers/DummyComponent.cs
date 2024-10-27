using System;
using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Components;
using ductwork.Executors;

namespace ductworkTests.TestHelpers;

public record DummyComponent : Component
{
    public readonly OutputPlug Out = new();
    public readonly InputPlug In = new();
    
    public Setting<int> DummyInt = default;
    public Setting<string> DummyString = string.Empty;
    public Setting<int[]> DummyIntArray = new();
    public Setting<object[]> DummyObjectArray = new();

    public override Task Execute(IExecutor executor, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}