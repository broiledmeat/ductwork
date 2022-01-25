using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using ductwork.Components;
using ductwork.Executors;
using NLog;

#nullable enable
namespace ductwork.Builders;

public class Graph
{
    public readonly string DisplayName;
    public readonly Logger Logger;
    public readonly ReadOnlyCollection<Component> Components;
    public readonly ReadOnlyCollection<(OutputPlug, InputPlug)> Connections;
    
    public Graph(
        string displayName,
        Logger logger,
        IEnumerable<Component> components,
        IEnumerable<(OutputPlug, InputPlug)> connections)
    {
        DisplayName = displayName;
        Logger = logger;
        Components = new ReadOnlyCollection<Component>(components.ToArray());
        Connections = new ReadOnlyCollection<(OutputPlug, InputPlug)>(connections.ToArray());
    }

    public T GetExecutor<T>() where T : IExecutor
    {
        var constructor = typeof(T).GetConstructor(new[]
        {
            typeof(string),
            typeof(Logger),
            typeof(ICollection<Component>),
            typeof(ICollection<(OutputPlug, InputPlug)>)
        });

        Debug.Assert(
            constructor != null,
            $"{nameof(constructor)} requires a default {nameof(IExecutor)} constructor");

        return (T) constructor.Invoke(new object[]
        {
            DisplayName,
            Logger,
            Components,
            Connections
        });
    }
}