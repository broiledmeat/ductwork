using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ductwork.Components;
using ductwork.Executors;
using NLog;
using NLog.Config;
using NLog.Targets;

[assembly: InternalsVisibleTo("ductworkTests")]
#nullable enable
namespace ductwork;

public class GraphBuilder
{
    public const string DefaultLogFormat = "${longdate} ${level:uppercase=true}: ${message} ${exception}";

    private readonly HashSet<Component> _components = new();

    private readonly HashSet<(Component, OutputPlug)> _componentOutputs = new();
    private readonly HashSet<(Component, InputPlug)> _componentInputs = new();

    private readonly HashSet<(object, FieldInfo)> _plugFieldInfos = new();

    private readonly HashSet<(OutputPlug, InputPlug)> _connections = new();

    private Logger? _logger;

    public string DisplayName;

    static GraphBuilder()
    {
        var config = new LoggingConfiguration();
        config.AddRule(
            LogLevel.Trace,
            LogLevel.Fatal,
            new ColoredConsoleTarget {Layout = DefaultLogFormat});
        LogManager.Configuration = config;
    }

    public GraphBuilder(string? displayName = null)
    {
        DisplayName = displayName ?? Guid.NewGuid().ToString();
    }

    public Logger Log => _logger ??= LogManager.GetLogger($"{GetType().FullName}_{DisplayName}");

    public T GetExecutor<T>() where T : GraphExecutor
    {
        var constructor = typeof(T).GetConstructor(new[]
        {
            typeof(string),
            typeof(Logger),
            typeof(ICollection<Component>),
            typeof(ICollection<(Component, OutputPlug)>),
            typeof(ICollection<(Component, InputPlug)>),
            typeof(ICollection<(object, FieldInfo)>),
            typeof(ICollection<(OutputPlug, InputPlug)>)
        });

        Debug.Assert(constructor != null, nameof(constructor) + " != null");

        return (T) constructor.Invoke(new object[]
        {
            DisplayName,
            Log,
            _components,
            _componentOutputs,
            _componentInputs,
            _plugFieldInfos,
            _connections
        });
    }

    public void Add(params Component[] components)
    {
        static Dictionary<FieldInfo, T> GetFieldsOfType<T>(Component obj)
        {
            var type = obj.GetType();
            return type
                .GetFields()
                .Where(info => info.FieldType.IsAssignableTo(typeof(T)))
                .ToDictionary(
                    assignableField => assignableField,
                    assignableField => (T) type.GetField(assignableField.Name)!.GetValue(obj)!);
        }

        foreach (var component in components)
        {
            _components.Add(component);

            Log.Debug($"Added component {component.DisplayName}<{component.GetType().Name}>");

            var outputs = GetFieldsOfType<OutputPlug>(component);
            var inputs = GetFieldsOfType<InputPlug>(component);

            foreach (var (field, plug) in outputs)
            {
                _plugFieldInfos.Add((plug, field));
            }

            foreach (var (field, plug) in inputs)
            {
                _plugFieldInfos.Add((plug, field));
            }

            outputs.Values.ForEach(output => _componentOutputs.Add((component, output)));
            inputs.Values.ForEach(input => _componentInputs.Add((component, input)));
        }
    }

    public void Connect(OutputPlug output, InputPlug input)
    {
        var outputComponent = _componentOutputs
            .Where(pair => pair.Item2.Equals(output))
            .Select(pair => pair.Item1)
            .FirstOrDefault();
        var inputComponent = _componentInputs
            .Where(pair => pair.Item2.Equals(input))
            .Select(pair => pair.Item1)
            .FirstOrDefault();

        if (outputComponent == null)
        {
            throw new InvalidOperationException("Output plugs' component has not been added to the graph.");
        }

        if (inputComponent == null)
        {
            throw new InvalidOperationException("Input plugs' component has not been added to the graph.");
        }

        _connections.Add((output, input));

        var outputFieldName = _plugFieldInfos
            .Where(pair => pair.Item1.Equals(output))
            .Select(pair => pair.Item2)
            .FirstOrDefault()
            ?.Name ?? "Out";
        var inputFieldName = _plugFieldInfos
            .Where(pair => pair.Item1.Equals(input))
            .Select(pair => pair.Item2)
            .FirstOrDefault()
            ?.Name ?? "In";

        Log.Debug($"Connected {outputComponent.DisplayName}.{outputFieldName} -> " +
                  $"{inputComponent.DisplayName}.{inputFieldName}");
    }

    #region Testing internals

    internal IEnumerable<Component> GetComponents()
    {
        return _components;
    }

    internal IEnumerable<(OutputPlug, InputPlug)> GetConnections()
    {
        return _connections;
    }

    #endregion
}