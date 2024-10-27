using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Crates;
using ductwork.Resources;
using ductwork.TaskRunners;
using NLog;

namespace ductwork.Executors;

public class ThreadedExecutor : IExecutor
{
    private readonly HashSet<Component> _components;
    private readonly HashSet<(Component, OutputPlug)> _componentOutputs;
    private readonly HashSet<(object, FieldInfo)> _fieldInfos;
    private readonly HashSet<(OutputPlug, InputPlug)> _connections;
    private readonly Dictionary<InputPlug, AsyncQueue<object?>> _inputQueues = new();
    private readonly HashSet<InputPlug> _inputsCompleted = [];
    private readonly HashSet<OutputPlug> _outputsCompleted = [];
    private readonly object _componentLock = new();
    private readonly object _resourceLock = new();
    private readonly HashSet<IResource> _resources = [];
    private ThreadedTaskRunner? _runner;

    public int MaximumParallelRunnerTasks = -1;

    public ThreadedExecutor(
        string displayName,
        Logger logger,
        ICollection<Component> components,
        ICollection<(OutputPlug, InputPlug)> connections)
    {
        DisplayName = displayName;
        Log = logger;

        _components = components.ToHashSet();

        _componentOutputs = components
            .SelectMany(component => component
                .GetFields<OutputPlug>()
                .Select(result => (component, result.Value)))
            .ToHashSet();

        var outputFieldInfos = components
            .SelectMany(component => component.GetFields<OutputPlug>())
            .Select(result => ((object) result.Value, result.Info));
        var inputFieldInfos = components
            .SelectMany(component => component.GetFields<InputPlug>())
            .Select(result => ((object) result.Value, result.Info));
        _fieldInfos = outputFieldInfos.Concat(inputFieldInfos).ToHashSet();

        _connections = connections.ToHashSet();
        _connections
            .Select(plugs => plugs.Item2)
            .Distinct()
            .ForEach(input => _inputQueues.Add(input, new AsyncQueue<object?>()));

        // Find any component inputs without any Output->Input connections, and add them to the completed set.
        var orphanInputs = components
            .SelectMany(component => component.GetFields<InputPlug>())
            .Select(fieldInfo => fieldInfo.Value)
            .Except(_connections.Select(pair => pair.Item2))
            .ToHashSet();
        _inputsCompleted.UnionWith(orphanInputs);
    }

    public string DisplayName { get; }

    public Logger Log { get; }

    public TaskRunner Runner => _runner ??= new ThreadedTaskRunner(MaximumParallelRunnerTasks);

    public async Task Execute(CancellationToken token)
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

        lock (_componentLock)
        {
            var outputs = GetOutputPlugs(component).ToArray();

            foreach (var output in outputs)
            {
                _outputsCompleted.Add(output);
            }

            var allConnectedInputs = outputs.SelectMany(GetInputPlugs);

            foreach (var input in allConnectedInputs)
            {
                var connectionsComplete = _connections
                    .Where(pair => pair.Item2.Equals(input))
                    .Select(pair => pair.Item1)
                    .All(connectedOutput => _outputsCompleted.Contains(connectedOutput));

                if (connectionsComplete)
                {
                    _inputsCompleted.Add(input);
                }
            }
        }

        Log.Debug($"Finished executing component {component.DisplayName}");
    }

    public ICrate CreateCrate(params IArtifact[] artifacts)
    {
        return new Crate(artifacts);
    }

    public ICrate CreateCrate(ICrate baseCrate, params IArtifact[] artifacts)
    {
        return new Crate(baseCrate, artifacts);
    }

    public async Task Push(OutputPlug output, ICrate crate)
    {
        var component = _componentOutputs
            .Where(pair => pair.Item2.Equals(output))
            .Select(pair => pair.Item1)
            .FirstOrDefault();

        if (component == null)
        {
            return;
        }

        var outputFieldName = _fieldInfos
            .Where(pair => pair.Item1.Equals(output))
            .Select(pair => pair.Item2)
            .FirstOrDefault()
            ?.Name ?? "?";
        Log.Debug($"Plug {component.DisplayName}.{outputFieldName} pushed {crate}");

        if (!_connections.Any(pair => pair.Item1.Equals(output)))
        {
            return;
        }

        var tasks = _connections
            .Where(pair => pair.Item1.Equals(output))
            .Select(pair => pair.Item2)
            .Select(input => _inputQueues[input].Enqueue(crate));
        await Task.WhenAll(tasks);
    }

    public async Task<ICrate> Get(InputPlug input, CancellationToken token)
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

            return (ICrate) (await queue.Dequeue(token))!;
        }
    }

    public int Count(InputPlug input) => _inputQueues.GetValueOrDefault(input)?.Count ?? 0;

    public bool IsFinished(InputPlug input) => Count(input) == 0 && _inputsCompleted.Contains(input);

    private IEnumerable<OutputPlug> GetOutputPlugs(Component component)
    {
        return _componentOutputs
            .Where(pair => pair.Item1.Equals(component))
            .Select(pair => pair.Item2)
            .Distinct();
    }

    private IEnumerable<InputPlug> GetInputPlugs(OutputPlug output)
    {
        return _connections
            .Where(pair => pair.Item1.Equals(output))
            .Select(pair => pair.Item2)
            .Distinct();
    }

    public virtual T GetResource<T>() where T : IResource
    {
        lock (_resourceLock)
        {
            var resource = _resources.FirstOrDefault(resource => resource.GetType() == typeof(T));

            if (resource != null)
            {
                return (T) resource;
            }

            var constructor = typeof(T).GetConstructor([])
                              ?? throw new Exception(
                                  $"Resource type `${typeof(T).Name}` does not have an empty constructor");
            resource = (T) constructor.Invoke([]);
            _resources.Add(resource);

            return (T) resource;
        }
    }
}