using System;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Executors;

#nullable enable
namespace ductwork.Components;

public class FinalizedResult : IArtifact
{
    public enum FinalizedState
    {
        Failed,
        Succeeded,
        SucceededSkipped,
    }

    public IFinalizingArtifact Artifact;
    public FinalizedState State;
    public Exception? Exception;

    public FinalizedResult(IFinalizingArtifact artifact, FinalizedState state, Exception? exception)
    {
        Artifact = artifact;
        State = state;
        Exception = exception;
    }
}

public class FinalizeArtifactsComponent : SingleInComponent
{
    public readonly OutputPlug Out = new();

    protected override async Task ExecuteIn(GraphExecutor graph, IArtifact artifact, CancellationToken token)
    {
        if (artifact is not IFinalizingArtifact finalizingArtifact)
        {
            return;
        }
        
        var state = FinalizedResult.FinalizedState.SucceededSkipped;
        Exception? exception = null;

        if (finalizingArtifact.RequiresFinalize())
        {
            try
            {
                state = await finalizingArtifact.Finalize(token)
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
            graph.Log.Info($"Finalized {DisplayName} {finalizingArtifact}");
        }
        else if (state == FinalizedResult.FinalizedState.SucceededSkipped)
        {
            graph.Log.Info($"Skipped finalizing {DisplayName} {finalizingArtifact}");
        }
        else if (exception != null)
        {
            graph.Log.Error(exception, $"Failed finalizing {DisplayName} {finalizingArtifact}");
        }
        else
        {
            graph.Log.Warn($"Failed finalizing {DisplayName} {finalizingArtifact}");
        }

        var resultArtifact = new FinalizedResult(finalizingArtifact, state, exception);
        await graph.Push(Out, resultArtifact);
    }
}