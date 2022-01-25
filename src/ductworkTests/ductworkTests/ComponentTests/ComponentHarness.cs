using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Executors;
using ductwork.TaskRunners;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace ductworkTests.ComponentTests;

public class ComponentHarness
{
    private readonly HarnessExecutor _executor;
    private readonly List<(InputPlug, IArtifact)> _queuedPushes = new();

    public ComponentHarness(Component component)
    {
        _executor = new HarnessExecutor($"Harness<{component.DisplayName}>", component);
    }

    public void QueuePush(InputPlug input, IArtifact value)
    {
        _queuedPushes.Add((input, value));
    }

    public ReadOnlyDictionary<OutputPlug, IArtifact[]> Execute()
    {
        foreach (var (input, artifact) in _queuedPushes)
        {
            _executor.Push(input, artifact).Wait();
        }

        _executor.Execute(CancellationToken.None).Wait();

        return _executor.GetOutputArtifacts();
    }

    private class HarnessExecutor : IExecutor
    {
        private readonly Component _component;
        private readonly ConcurrentDictionary<OutputPlug, ConcurrentBag<IArtifact>> _outputArtifacts = new();
        private readonly ConcurrentDictionary<InputPlug, AsyncQueue<object?>> _inputQueues = new();
        private TaskRunner? _runner;

        static HarnessExecutor()
        {
            var config = new LoggingConfiguration();
            config.AddRule(
                LogLevel.Trace,
                LogLevel.Fatal,
                new ColoredConsoleTarget {Layout = GraphBuilder.DefaultLogFormat});
            LogManager.Configuration = config;
        }

        public HarnessExecutor(string displayName, Component component)
            : base(
                displayName,
                LogManager.GetLogger(displayName),
                Array.Empty<Component>(),
                Array.Empty<(Component, OutputPlug)>(),
                Array.Empty<(Component, InputPlug)>(),
                Array.Empty<(object, FieldInfo)>(),
                Array.Empty<(OutputPlug, InputPlug)>())
        {
            _component = component;
        }

        public ReadOnlyDictionary<OutputPlug, IArtifact[]> GetOutputArtifacts()
        {
            return new ReadOnlyDictionary<OutputPlug, IArtifact[]>(
                _outputArtifacts.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
        }

        public override TaskRunner Runner => _runner ??= new ThreadedTaskRunner(1);

        public override async Task Execute(CancellationToken token)
        {
            await _component.Execute(this, token);
        }
        
        public override Task Push(OutputPlug output, IArtifact artifact)
        {
            if (!_outputArtifacts.ContainsKey(output))
            {
                _outputArtifacts[output] = new ConcurrentBag<IArtifact>();
            }

            _outputArtifacts[output].Add(artifact);

            return Task.CompletedTask;
        }

        public async Task Push(InputPlug input, IArtifact artifact)
        {
            if (!_inputQueues.ContainsKey(input))
            {
                _inputQueues[input] = new AsyncQueue<object?>();
            }

            await _inputQueues[input].Enqueue(artifact);
        }

        public override async Task<IArtifact> Get(InputPlug input, CancellationToken token)
        {
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

        public override bool IsFinished(InputPlug input) => Count(input) == 0;
    }
}