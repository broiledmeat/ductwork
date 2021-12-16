using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;

namespace ductworkTests.ComponentTests;

public class ComponentHarness
{
    private readonly HarnessGraph _graph;
    private readonly Component _component;
    private readonly Dictionary<InputPlug, List<IArtifact>> _queuedPushes;
    private readonly Dictionary<OutputPlug, ValueReceiverComponent> _outputReceivers;

    public ComponentHarness(Component component)
    {
        _graph = new HarnessGraph {DisplayName = $"Harness<{component.DisplayName}"};
        _component = component;
        
        _graph.Add(_component);
        
        _queuedPushes = _graph.GetInputPlugs(_component)
            .ToDictionary(input => input, _ => new List<IArtifact>());
        
        _outputReceivers = _graph.GetOutputPlugs(_component)
            .ToDictionary(
                output => output,
                output =>
                {
                    var receiver = new ValueReceiverComponent();
                    _graph.Add(receiver);
                    _graph.Connect(output, receiver.In);
                    return receiver;
                });
    }

    public void QueuePush(InputPlug input, IArtifact value)
    {
        _queuedPushes[input].Add(value);
    }

    public ReadOnlyDictionary<OutputPlug, IArtifact[]> Execute()
    {
        _outputReceivers.Values.ForEach(receiver => receiver.Clear());

        foreach (var (input, values) in _queuedPushes)
        {
            foreach (var artifact in values)
            {
                _graph.Push(input, artifact).Wait();
            }
        }

        foreach (var input in _graph.GetInputPlugs(_component))
        {
            _graph.SetIsFinished(input);
        }
        
        _graph.Execute().Wait();

        return new ReadOnlyDictionary<OutputPlug, IArtifact[]>(_outputReceivers
            .ToDictionary(pair => pair.Key, pair => pair.Value.Values.ToArray()));
    }

    private class HarnessGraph : Graph
    {
        public async Task Push(InputPlug input, IArtifact value)
        {
            var queue = GetInputQueues().GetValueOrDefault(input);
            
            if (queue == null)
            {
                return;
            }

            await queue.Enqueue(value);
        }

        public void SetIsFinished(InputPlug input)
        {
            GetInputsCompleted().Add(input);
        }
    }

    private class ValueReceiverComponent : SingleInComponent
    {
        private readonly object _lock = new();
        private readonly List<IArtifact> _values = new();
        
        public readonly ReadOnlyCollection<IArtifact> Values;

        public ValueReceiverComponent()
        {
            Values = new ReadOnlyCollection<IArtifact>(_values);
        }
        
        protected override Task ExecuteIn(Graph graph, IArtifact artifact, CancellationToken token)
        {
            lock (_lock)
            {
                _values.Add(artifact);
            }
            
            return Task.CompletedTask;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _values.Clear();
            }
        }
    }
}