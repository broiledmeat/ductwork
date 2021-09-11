using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork
{
    public class ExecutionContext<T>
    {
        public readonly Graph Graph;
        public readonly Component<T> Component;

        public ExecutionContext(Graph graph, Component<T> component)
        {
            Graph = graph;
            Component = component;
        }

        public async Task PushResult(object key, T value)
        {
            var addTasks = Graph.GetPlugs<T>(Component, key).Select(plug => plug.Add(value));
            await Task.WhenAll(addTasks);
        }

        public async Task PushResult(T value)
        {
            await PushResult(Graph.DefaultKey, value);
        }
    }

    public interface IComponent
    {
        Type Type { get; }
        Task ExecuteWithGraph(Graph graph, CancellationToken token);
    }

    public abstract class Component<T> : IComponent
    {
        public Type Type => typeof(T);

        public async Task ExecuteWithGraph(Graph graph, CancellationToken token)
        {
            var context = new ExecutionContext<T>(graph, this);
            await Execute(context, token);
        }

        public abstract Task Execute(ExecutionContext<T> context, CancellationToken token);

        public override string ToString()
        {
            const string removeSuffix = "Component";
            var name = GetType().Name;
            name = name.EndsWith(removeSuffix) ? name[..^removeSuffix.Length] : name;
            return $"{name}<{Type.Name}>";
        }
    }

    public abstract class SingleInExecutorComponent<T, TU> : Component<T>
    {
        public readonly Plug<TU> In = new();

        public override async Task Execute(ExecutionContext<T> context, CancellationToken token)
        {
            var runner = new TaskRunner();

            while (!In.IsFinished)
            {
                token.ThrowIfCancellationRequested();

                if (In.Count == 0)
                {
                    await Task.Delay(50, token);
                    continue;
                }

                var value = await In.Get(token);
                await runner.RunAsync(() => ExecuteIn(context, value, token), token);
            }

            await runner.WaitAsync();
            await ExecuteComplete(context, token);
        }

        public abstract Task ExecuteIn(ExecutionContext<T> context, TU value, CancellationToken token);

        public virtual Task ExecuteComplete(ExecutionContext<T> context, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}