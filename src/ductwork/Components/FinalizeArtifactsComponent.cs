using System;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;

#nullable enable
namespace ductwork.Components
{
    public class FinalizedResult
    {
        public enum FinalizedState
        {
            Failed,
            Succeeded,
            SucceededSkipped,
        }

        public Artifact Artifact;
        public FinalizedState State;
        public Exception? Exception;

        public FinalizedResult(Artifact artifact, FinalizedState state, Exception? exception)
        {
            Artifact = artifact;
            State = state;
            Exception = exception;
        }
    }

    public class FinalizeArtifactsComponent : SingleInComponent<Artifact>
    {
        public readonly OutputPlug<FinalizedResult> Out = new();
        
        public override async Task ExecuteIn(Graph graph, Artifact value, CancellationToken token)
        {
            var state = FinalizedResult.FinalizedState.SucceededSkipped;
            Exception? exception = null;

            if (value.RequiresFinalize())
            {
                try
                {
                    state = await value.Finalize(token)
                        ? FinalizedResult.FinalizedState.Succeeded
                        : FinalizedResult.FinalizedState.Failed;
                }
                catch (Exception e)
                {
                    state = FinalizedResult.FinalizedState.Failed;
                    exception = e;
                }
            }

            if (state == FinalizedResult.FinalizedState.Succeeded)
            {
                graph.Log.Info($"Finalized {DisplayName} {value}");
            }
            else if (state == FinalizedResult.FinalizedState.SucceededSkipped)
            {
                graph.Log.Info($"Skipped finalizing {DisplayName} {value}");
            }
            else if (exception != null)
            {
                graph.Log.Error(exception, $"Failed finalizing {DisplayName} {value}");
            }
            else
            {
                graph.Log.Warn($"Failed finalizing {DisplayName} {value}");
            }

            var result = new FinalizedResult(value, state, exception);
            await graph.Push(Out, result);
        }
    }
}