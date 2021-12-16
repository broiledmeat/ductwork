using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Resources;
using NLog;
using NLog.Config;
using NLog.Targets;

[assembly: InternalsVisibleTo("ductworkTests")]
#nullable enable
namespace ductwork;

public class Graph
{
    private readonly object _lock = new();

    private readonly HashSet<IResource> _resources = new();

    private readonly HashSet<Component> _components = new();

    private readonly Dictionary<Component, HashSet<OutputPlug>> _componentOutputs = new();
    private readonly Dictionary<Component, HashSet<InputPlug>> _componentInputs = new();

    private readonly Dictionary<object, FieldInfo> _plugFieldInfos = new();

    private readonly Dictionary<OutputPlug, HashSet<InputPlug>> _connections = new();
    private readonly Dictionary<InputPlug, AsyncQueue<object?>> _inputQueues = new();

    private readonly HashSet<OutputPlug> _outputsCompleted = new();
    private readonly HashSet<InputPlug> _inputsCompleted = new();

    public const string DefaultLogFormat = "${longdate} ${level:uppercase=true}: ${message} ${exception}";
    public readonly Logger Log;

    public string DisplayName { get; set; } = Guid.NewGuid().ToString();

    static Graph()
    {
        var config = new LoggingConfiguration();
        config.AddRule(LogLevel.Trace, LogLevel.Fatal, new ColoredConsoleTarget {Layout = DefaultLogFormat});
        LogManager.Configuration = config;
    }

    public Graph()
    {
        Log = LogManager.GetLogger($"{GetType().FullName}_{DisplayName}");
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
                _plugFieldInfos.Add(plug, field);
            }

            foreach (var (field, plug) in inputs)
            {
                _plugFieldInfos.Add(plug, field);
            }

            _componentOutputs.Add(component, outputs.Values.ToHashSet());
            _componentInputs.Add(component, inputs.Values.ToHashSet());

            foreach (var input in inputs.Values)
            {
                _inputQueues.Add(input, new AsyncQueue<object?>());
            }
        }
    }

    public void Connect(OutputPlug output, InputPlug input)
    {
        var outputComponent = _componentOutputs
            .Where(pair => pair.Value.Contains(output))
            .Select(pair => pair.Key)
            .FirstOrDefault();
        var inputComponent = _componentInputs
            .Where(pair => pair.Value.Contains(input))
            .Select(pair => pair.Key)
            .FirstOrDefault();

        if (outputComponent == null)
        {
            throw new InvalidOperationException("Output plugs' component has not been added to the graph.");
        }

        if (inputComponent == null)
        {
            throw new InvalidOperationException("Input plugs' component has not been added to the graph.");
        }

        if (!_connections.ContainsKey(output))
        {
            _connections.Add(output, new HashSet<InputPlug>());
        }

        _connections[output].Add(input);

        var outputFieldName = _plugFieldInfos.GetValueOrDefault(output)?.Name ?? "Out";
        var inputFieldName = _plugFieldInfos.GetValueOrDefault(input)?.Name ?? "In";

        Log.Debug($"Connected {outputComponent.DisplayName}.{outputFieldName} -> " +
                  $"{inputComponent.DisplayName}.{inputFieldName}");
    }

    public async Task Execute(CancellationToken token = default)
    {
        Log.Debug($"Started executing graph {DisplayName}");
        var componentTasks = _components
            .Select(component => Task.Run(() => ExecuteComponent(component, token).Wait(token), token))
            .ToArray();
        await Task.WhenAll(componentTasks);
        Log.Debug($"Finished executing graph {DisplayName}");
    }

    private async Task ExecuteComponent(Component component, CancellationToken token)
    {
        Log.Debug($"Started executing component {component.DisplayName}");

        try
        {
            await component.Execute(this, token);
        }
        catch (Exception e)
        {
            Log.Error(e, $"Exception executing {component.DisplayName}");
        }

        lock (_lock)
        {
            var outputs = GetOutputPlugs(component).ToArray();

            foreach (var output in outputs)
            {
                _outputsCompleted.Add(output);
            }

            var allConnectedInputs = outputs
                .Select(output => _connections.GetValueOrDefault(output))
                .NotNull()
                .SelectMany(inputs => inputs!)
                .ToHashSet();

            foreach (var input in allConnectedInputs)
            {
                var connectionsComplete = _connections
                    .Where(outputPair => outputPair.Value.Contains(input))
                    .Select(outputPair => outputPair.Key)
                    .All(connectedOutput => _outputsCompleted.Contains(connectedOutput));

                if (connectionsComplete)
                {
                    _inputsCompleted.Add(input);
                }
            }
        }

        Log.Debug($"Finished executing component {component.DisplayName}");
    }

    public async Task Push(OutputPlug output, IArtifact value)
    {
        var component = _componentOutputs
            .Where(pair => pair.Value.Contains(output))
            .Select(pair => pair.Key)
            .FirstOrDefault();

        if (component == null || !_connections.ContainsKey(output))
        {
            return;
        }

        var tasks = _connections[output]
            .Select(input => _inputQueues.GetValueOrDefault(input))
            .NotNull()
            .Select(queue => queue!.Enqueue(value));
        await Task.WhenAll(tasks);

        var outputFieldName = _plugFieldInfos.GetValueOrDefault(output)?.Name ?? "Out";
        Log.Debug($"Plug {component.DisplayName}.{outputFieldName} pushed: {value.ToString()}");
    }

    public async Task<IArtifact> Get(InputPlug input, CancellationToken token = default)
    {
        if (!_inputQueues.ContainsKey(input))
        {
            throw new InvalidOperationException();
        }

        var queue = _inputQueues[input];

        while (true)
        {
            token.ThrowIfCancellationRequested();

            if (queue.Count == 0)
            {
                await Task.Delay(50, token);
                continue;
            }

            return (IArtifact) (await queue.Dequeue(token))!;
        }
    }

    public int Count(InputPlug input)  => _inputQueues.GetValueOrDefault(input)?.Count ?? 0;

    public bool IsFinished(InputPlug input) => Count(input) == 0 && _inputsCompleted.Contains(input);

    public T GetResource<T>() where T : IResource
    {
        lock (_lock)
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

    #region Testing internals

    internal Dictionary<InputPlug, AsyncQueue<object?>> GetInputQueues()
    {
        return _inputQueues;
    }

    internal HashSet<InputPlug> GetInputsCompleted()
    {
        return _inputsCompleted;
    }

    internal IEnumerable<Component> GetComponents()
    {
        return _components;
    }

    internal IEnumerable<(OutputPlug, InputPlug)> GetConnections()
    {
        foreach (var (output, inputs) in _connections)
        {
            foreach (var input in inputs)
            {
                yield return (output, input);
            }
        }
    }

    internal IEnumerable<OutputPlug> GetOutputPlugs(Component component)
    {
        return _componentOutputs.GetValueOrDefault(component) ?? Enumerable.Empty<OutputPlug>();
    }

    internal IEnumerable<InputPlug> GetInputPlugs(Component component)
    {
        return _componentInputs.GetValueOrDefault(component) ?? Enumerable.Empty<InputPlug>();
    }

    #endregion
}