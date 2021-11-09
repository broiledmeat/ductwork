using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork
{
    public abstract class Component
    {
        public abstract Task Execute(Graph graph, CancellationToken token);
        
        public override string ToString()
        {
            const string removeSuffix = nameof(Component);
            var name = GetType().Name;
            name = name.EndsWith(removeSuffix) ? name[..^removeSuffix.Length] : name;
            return name;
        }
    }
    
    public abstract class SingleInComponent<TI> : Component
    {
        private const int InputWaitMs = 50;
        
        public readonly InputPlug<TI> In = new();

        public override async Task Execute(Graph graph, CancellationToken token)
        {
            var runner = new TaskRunner();
        
            while (!graph.IsFinished(In))
            {
                token.ThrowIfCancellationRequested();
        
                if (graph.Count(In) == 0)
                {
                    await Task.Delay(InputWaitMs, token);
                    continue;
                }
        
                var value = await graph.Get(In, token);
                await runner.RunAsync(() => ExecuteIn(graph, value, token), token);
            }
        
            await runner.WaitAsync();
            await ExecuteComplete(graph, token);
        }

        public abstract Task ExecuteIn(Graph graph, TI value, CancellationToken token);
        
        public virtual Task ExecuteComplete(Graph graph, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }

    public abstract class SingleInSingleOutComponent<TI, TO> : SingleInComponent<TI>
    {
        public readonly OutputPlug<TO> Out = new();
    }

    public abstract class SingleOutComponent<TO> : Component
    {
        public readonly OutputPlug<TO> Out = new();
    }
}