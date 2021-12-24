using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.TaskRunners;
using NLog;

namespace ductwork.Executors;

#nullable enable
public class ThreadedExecutor : GraphExecutor
{
    private readonly HashSet<Component> _components;
    private readonly HashSet<(Component, OutputPlug)> _componentOutputs;
    private readonly HashSet<(object, FieldInfo)> _fieldInfos;
    private readonly HashSet<(OutputPlug, InputPlug)> _connections;
    private readonly Dictionary<InputPlug, AsyncQueue<object?>> _inputQueues = new();
    private readonly HashSet<InputPlug> _inputsCompleted = new();
    private readonly HashSet<OutputPlug> _outputsCompleted = new();
    private readonly object _componentLock = new();
    private ThreadedTaskRunner? _runner;

    public ThreadedExecutor(
        string displayName,
        Logger logger,
        ICollection<Component> components,
        ICollection<(Component, OutputPlug)> componentOutputs,
        ICollection<(Component, InputPlug)> componentInputs,
        ICollection<(object, FieldInfo)> fieldInfos,
        ICollection<(OutputPlug, InputPlug)> connections)
        : base(displayName, logger, components, componentOutputs, componentInputs, fieldInfos, connections)
    {
        _components = components.ToHashSet();
        _componentOutputs = componentOutputs.ToHashSet();
        _fieldInfos = fieldInfos.ToHashSet();
        _connections = connections.ToHashSet();

        _connections
            .Select(plugs => plugs.Item2)
            .Distinct()
            .ForEach(input => _inputQueues.Add(input, new AsyncQueue<object?>()));
    }

    public int MaximumParallelRunnerTasks = -1;

    public override TaskRunner Runner => _runner ??= new ThreadedTaskRunner(MaximumParallelRunnerTasks);

    public override async Task Execute(CancellationToken token)
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
    
    public override async Task Push(OutputPlug output, IArtifact artifact)
    {
        var component = _componentOutputs
            .Where(pair => pair.Item2.Equals(output))
            .Select(pair => pair.Item1)
            .FirstOrDefault();

        if (component == null || !_connections.Any(pair => pair.Item1.Equals(output)))
        {
            return;
        }

        var tasks = _connections
            .Where(pair => pair.Item1.Equals(output))
            .Select(pair => pair.Item2)
            .Select(input => _inputQueues[input].Enqueue(artifact));
        await Task.WhenAll(tasks);

        var outputFieldName = _fieldInfos
            .Where(pair => pair.Item1.Equals(output))
            .Select(pair => pair.Item2)
            .FirstOrDefault()
            ?.Name ?? "Out";
        Log.Debug($"Plug {component.DisplayName}.{outputFieldName} pushed: {artifact.ToString()}");
    }

    public override async Task<IArtifact> Get(InputPlug input, CancellationToken token)
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

    public override int Count(InputPlug input) => _inputQueues.GetValueOrDefault(input)?.Count ?? 0;

    public override bool IsFinished(InputPlug input) => Count(input) == 0 && _inputsCompleted.Contains(input);

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
}