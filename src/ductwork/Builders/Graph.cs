using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public IEnumerable<Exception> Validate()
    {
        var outputPlugs = Components
            .ToDictionary(
                component => component,
                component => component.GetFields<OutputPlug>().Select(fieldResult => fieldResult.Value).ToArray());
        var inputPlugs = Components
            .ToDictionary(
                component => component,
                component => component.GetFields<InputPlug>().Select(fieldResult => fieldResult.Value).ToArray());

        foreach (var connection in Connections)
        {
            var outputComponent = outputPlugs
                .Where(pair => pair.Value.Contains(connection.Item1))
                .Select(pair => pair.Key)
                .FirstOrDefault();

            if (outputComponent == null)
            {
                yield return new InvalidOperationException("Connection output component was not in the graph.");
            }
            
            var inputComponent = inputPlugs
                .Where(pair => pair.Value.Contains(connection.Item2))
                .Select(pair => pair.Key)
                .FirstOrDefault();
            
            if (inputComponent == null)
            {
                yield return new InvalidOperationException("Connection input component was not in the graph.");
            }
        }
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

        if (constructor == null)
        {
            throw new ArgumentException($"{typeof(T)} requires a default {nameof(IExecutor)} constructor.");
        }

        return (T) constructor.Invoke(new object[]
        {
            DisplayName,
            Logger,
            Components,
            Connections
        });
    }
}