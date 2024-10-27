using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ductwork.Components;
using ductwork.Executors;
using NLog;

namespace ductwork.Builders;

public class Graph(
    string displayName,
    Logger logger,
    IEnumerable<Component> components,
    IEnumerable<(OutputPlug, InputPlug)> connections)
{
    public readonly string DisplayName = displayName;
    public readonly Logger Logger = logger;
    public readonly ReadOnlyCollection<Component> Components = new(components.ToArray());
    public readonly ReadOnlyCollection<(OutputPlug, InputPlug)> Connections = new(connections.ToArray());

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
        var constructor = typeof(T).GetConstructor([
            typeof(string),
            typeof(Logger),
            typeof(ICollection<Component>),
            typeof(ICollection<(OutputPlug, InputPlug)>)
        ]);

        if (constructor == null)
        {
            throw new ArgumentException($"{typeof(T)} requires a default {nameof(IExecutor)} constructor.");
        }

        return (T) constructor.Invoke([
            DisplayName,
            Logger,
            Components,
            Connections
        ]);
    }
}