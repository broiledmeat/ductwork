using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Executors;
using ductwork.Resources;
using ductwork.TaskRunners;
using NLog;

#nullable enable
namespace ductworkTests.TestHelpers;

public class DummyExecutor : IExecutor
{
    public readonly ICollection<Component> Components;
    public readonly ICollection<(OutputPlug, InputPlug)> Connections;

    public DummyExecutor(
        string displayName,
        Logger logger,
        ICollection<Component> components,
        ICollection<(OutputPlug, InputPlug)> connections)
    {
        Components = components;
        Connections = connections;
        DisplayName = displayName;
        Log = logger;
        Runner = new DummyTaskRunner();
    }

    public string DisplayName { get; }
    public Logger Log { get; }
    public TaskRunner Runner { get; }

    public Task Execute(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task Push(OutputPlug output, IArtifact artifact)
    {
        throw new NotImplementedException();
    }

    public Task<IArtifact> Get(InputPlug input, CancellationToken token)
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