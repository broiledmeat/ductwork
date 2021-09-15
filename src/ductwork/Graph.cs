using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork
{
    public class Graph
    {
        public static readonly object DefaultKey = new();

        private readonly object _lock = new();

        private readonly HashSet<Component> _components = new();

        private readonly Dictionary<Component, HashSet<IOutputPlug>> _componentOutputs = new();
        private readonly Dictionary<Component, HashSet<IInputPlug>> _componentInputs = new();

        private readonly Dictionary<IOutputPlug, Dictionary<object, HashSet<IInputPlug>>> _connections = new();
        private readonly Dictionary<IInputPlug, AsyncQueue<object?>> _inputQueues = new();

        private readonly HashSet<IOutputPlug> _outputsCompleted = new();
        private readonly HashSet<IInputPlug> _inputsCompleted = new();

        public void Add(params Component[] components)
        {
            static IEnumerable<T> GetFieldsOfType<T>(Component obj)
            {
                var type = obj.GetType();
                return type
                    .GetFields()
                    .Where(info => info.FieldType.IsAssignableTo(typeof(T)))
                    .Select(info => type.GetField(info.Name)?.GetValue(obj))
                    .NotNull()
                    .Cast<T>()
                    .ToHashSet();
            }

            foreach (var component in components)
            {
                _components.Add(component);

                var outputs = GetFieldsOfType<IOutputPlug>(component).ToHashSet();
                var inputs = GetFieldsOfType<IInputPlug>(component).ToHashSet();

                _componentOutputs.Add(component, outputs);
                _componentInputs.Add(component, inputs);

                foreach (var input in inputs)
                {
                    _inputQueues.Add(input, new AsyncQueue<object?>());
                }
            }
        }

        public void Connect<T>(OutputPlug<T> output, InputPlug<T> inputPlug)
        {
            Connect(output, DefaultKey, inputPlug);
        }

        public void Connect<T>(OutputPlug<T> output, object key, InputPlug<T> input)
        {
            if (!_componentOutputs.Values.Any(outputs => outputs.Contains(output)))
            {
                throw new InvalidOperationException("Output plugs' component has not been added to the graph.");
            }
            
            if (!_componentInputs.Values.Any(inputs => inputs.Contains(input)))
            {
                throw new InvalidOperationException("Input plugs' component has not been added to the graph.");
            }
            
            if (output.Type != input.Type)
            {
                throw new InvalidOperationException(
                    $"Output type of {output.Type} does not match Input type of {input.Type}");
            }

            if (!_connections.ContainsKey(output))
            {
                _connections.Add(output, new Dictionary<object, HashSet<IInputPlug>>());
            }

            if (!_connections[output].ContainsKey(key))
            {
                _connections[output].Add(key, new HashSet<IInputPlug>());
            }

            _connections[output][key].Add(input);
        }

        public async Task Execute(CancellationToken token = default)
        {
            var componentTasks = _components
                .Select(component => Task.Run(() => ExecuteComponent(component, token).Wait(token), token))
                .ToArray();
            await Task.WhenAll(componentTasks);
        }

        private async Task ExecuteComponent(Component component, CancellationToken token)
        {
            await component.Execute(this, token);

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
                    .SelectMany(keyPair => keyPair.Values)
                    .SelectMany(inputs => inputs)
                    .ToHashSet();

                foreach (var input in allConnectedInputs)
                {
                    var connectionsComplete = _connections
                        .Where(outputPair => outputPair.Value.SelectMany(keyPair => keyPair.Value).Contains(input))
                        .Select(outputPair => outputPair.Key)
                        .All(connectedOutput => _outputsCompleted.Contains(connectedOutput));

                    if (connectionsComplete)
                    {
                        _inputsCompleted.Add(input);
                    }
                }
            }
        }

        private IEnumerable<IOutputPlug> GetOutputPlugs(Component component)
        {
            return _componentOutputs.GetValueOrDefault(component, new HashSet<IOutputPlug>());
        }

        private IEnumerable<IInputPlug> GetInputPlugs(Component component)
        {
            return _componentInputs.GetValueOrDefault(component, new HashSet<IInputPlug>());
        }

        public async Task Push<T>(OutputPlug<T> output, T value)
        {
            await Push(output, DefaultKey, value);
        }

        public async Task Push<T>(OutputPlug<T> output, object key, T value)
        {
            if (!_connections.ContainsKey(output) || !_connections[output].ContainsKey(key))
            {
                return;
            }

            var tasks = _connections[output][key]
                .Select(input => _inputQueues.GetValueOrDefault(input))
                .NotNull()
                .Select(queue => queue.Enqueue(value));
            await Task.WhenAll(tasks);
        }

        public async Task<T> Get<T>(InputPlug<T> input, CancellationToken token = default)
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

                return (T)await queue.Dequeue(token);
            }
        }

        public int Count<T>(InputPlug<T> input) => _inputQueues.GetValueOrDefault(input)?.Count ?? 0;
        
        public bool IsFinished<T>(InputPlug<T> input) => Count(input) == 0 && _inputsCompleted.Contains(input);
    }
}