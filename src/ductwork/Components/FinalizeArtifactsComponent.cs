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

    public class FinalizeArtifactsComponent : SingleInExecutorComponent<FinalizedResult, Artifact>
    {
        public delegate void OnArtifactFinalized(FinalizeArtifactsComponent sender, FinalizedResult result);

        public event OnArtifactFinalized? ArtifactFinalized;

        public override async Task ExecuteIn(
            ExecutionContext<FinalizedResult> context,
            Artifact value,
            CancellationToken token)
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

            var result = new FinalizedResult(value, state, exception);
            await context.PushResult(result);

            ArtifactFinalized?.Invoke(this, result);
        }
    }
}