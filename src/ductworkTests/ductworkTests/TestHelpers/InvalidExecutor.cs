using System;
using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Artifacts;
using ductwork.Crates;
using ductwork.Executors;
using ductwork.Resources;
using ductwork.TaskRunners;
using NLog;

#nullable enable
namespace ductworkTests.TestHelpers;

public class InvalidExecutor : IExecutor
{
    public string DisplayName => "";
    public Logger Log { get; } = new NullLogger(new LogFactory());
    public TaskRunner Runner { get; } = new DummyTaskRunner();

    public Task Execute(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ICrate CreateCrate(params IArtifact[] artifacts)
    {
        throw new NotImplementedException();
    }

    public ICrate CreateCrate(ICrate baseCrate, params IArtifact[] artifacts)
    {
        throw new NotImplementedException();
    }

    public Task Push(OutputPlug output, ICrate crate)
    {
        throw new NotImplementedException();
    }

    public Task<ICrate> Get(InputPlug input, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public int Count(InputPlug input)
    {
        throw new NotImplementedException();
    }

    public bool IsFinished(InputPlug input)
    {
        throw new NotImplementedException();
    }

    public T GetResource<T>() where T : IResource
    {
        throw new NotImplementedException();
    }
}