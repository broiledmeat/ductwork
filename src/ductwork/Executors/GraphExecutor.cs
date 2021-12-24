using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Resources;
using ductwork.TaskRunners;
using NLog;
using Component = ductwork.Components.Component;

#nullable enable
namespace ductwork.Executors;

public abstract class GraphExecutor
{
    private readonly object _resourceLock = new();
    private readonly HashSet<IResource> _resources = new();

    public readonly string DisplayName;
    public readonly Logger Log;

    public GraphExecutor(
        string displayName,
        Logger logger,
        ICollection<Component> components,
        ICollection<(Component, OutputPlug)> componentOutputs,
        ICollection<(Component, InputPlug)> componentInputs,
        ICollection<(object, FieldInfo)> fieldInfos,
        ICollection<(OutputPlug, InputPlug)> connections)
    {
        DisplayName = displayName;
        Log = logger;
    }
    
    public abstract TaskRunner Runner { get; } 

    public abstract Task Execute(CancellationToken token);

    public abstract Task Push(OutputPlug output, IArtifact value);

    public abstract Task<IArtifact> Get(InputPlug input, CancellationToken token);

    public abstract int Count(InputPlug input);

    public abstract bool IsFinished(InputPlug input);

    public virtual T GetResource<T>() where T : IResource
    {
        lock (_resourceLock)
        {
            var resource = _resources.FirstOrDefault(resource => resource.GetType() == typeof(T));

            if (resource != null)
            {
                return (T) resource;
            }

            var constructor = typeof(T).GetConstructor(Array.Empty<Type>())
                              ?? throw new Exception(
                                  $"Resource type `${typeof(T).Name}` does not have an empty constructor");
            resource = (T) constructor.Invoke(Array.Empty<object>());
            _resources.Add(resource);

            return (T) resource;
        }
    }
}