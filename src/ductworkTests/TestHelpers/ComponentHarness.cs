using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Crates;
using ductwork.Executors;
using ductwork.Resources;
using ductwork.TaskRunners;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace ductworkTests.TestHelpers;

public class ComponentHarness
{
    private readonly HarnessExecutor _executor;
    private readonly List<(InputPlug, ICrate)> _queuedPushes = new();

    public ComponentHarness(Component component)
    {
        _executor = new HarnessExecutor($"Harness<{component.DisplayName}>", component);
    }

    public ICrate CreateCrate(params IArtifact[] artifacts)
    {
        return _executor.CreateCrate(artifacts);
    }

    public void QueuePush(InputPlug input, ICrate value)
    {
        _queuedPushes.Add((input, value));
    }

    public ReadOnlyDictionary<OutputPlug, ICrate[]> Execute()
    {
        Task
            .Run(async () =>
            {
                var pushTasks = _queuedPushes
                    .Select(push => _executor.Push(push.Item1, push.Item2))
                    .ToArray();
                await Task.WhenAll(pushTasks);
                await _executor.Execute(CancellationToken.None);
            })
            .Wait();

        return _executor.GetOutputCrates();
    }

    private class HarnessExecutor : IExecutor
    {
        private readonly Component _component;
        private readonly ConcurrentDictionary<OutputPlug, ConcurrentBag<ICrate>> _outputCrates = new();
        private readonly ConcurrentDictionary<InputPlug, AsyncQueue<object?>> _inputQueues = new();
        private TaskRunner? _runner;

        static HarnessExecutor()
        {
            var config = new LoggingConfiguration();
            config.AddRule(
                LogLevel.Trace,
                LogLevel.Fatal,
                new ColoredConsoleTarget {Layout = Logging.DefaultLogFormat});
            LogManager.Configuration = config;
        }

        public HarnessExecutor(string displayName, Component component)
        {
            DisplayName = displayName;
            Log = LogManager.GetLogger(displayName);
            _component = component;
        }

        public ReadOnlyDictionary<OutputPlug, ICrate[]> GetOutputCrates()
        {
            return new ReadOnlyDictionary<OutputPlug, ICrate[]>(
                _outputCrates.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
        }

        public string DisplayName { get; }
        public Logger Log { get; }
        public TaskRunner Runner => _runner ??= new ThreadedTaskRunner(1);

        public async Task Execute(CancellationToken token)
        {
            await _component.Execute(this, token);
        }

        public ICrate CreateCrate(params IArtifact[] artifacts)
        {
            return new Crate(artifacts);
        }

        public ICrate CreateCrate(ICrate baseCrate, params IArtifact[] artifacts)
        {
            return new Crate(baseCrate, artifacts);
        }

        public Task Push(OutputPlug output, ICrate crate)
        {
            if (!_outputCrates.ContainsKey(output))
            {
                _outputCrates[output] = new ConcurrentBag<ICrate>();
            }

            _outputCrates[output].Add(crate);

            return Task.CompletedTask;
        }

        public async Task Push(InputPlug input, ICrate crate)
        {
            if (!_inputQueues.ContainsKey(input))
            {
                _inputQueues[input] = new AsyncQueue<object?>();
            }

            await _inputQueues[input].Enqueue(crate);
        }

        public async Task<ICrate> Get(InputPlug input, CancellationToken token)
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

                return (ICrate) (await queue.Dequeue(token))!;
            }
        }

        public int Count(InputPlug input) => _inputQueues.GetValueOrDefault(input)?.Count ?? 0;

        public bool IsFinished(InputPlug input) => Count(input) == 0;

        public T GetResource<T>() where T : IResource
        {
            throw new NotImplementedException();
        }
    }
}