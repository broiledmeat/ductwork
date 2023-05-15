using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Crates;
using ductwork.Executors;

#nullable enable
namespace ductwork.Components;

public class TransformSourcePathToTargetPathComponent : SingleInSingleOutComponent
{
    public Setting<string> SourceRoot = string.Empty;
    public Setting<string> TargetRoot = string.Empty;

    protected override Task ExecuteIn(IExecutor executor, ICrate crate, CancellationToken token)
    {
        if (crate.Get<SourcePathArtifact>() is not { } sourceFilePathArtifact)
        {
            return Task.CompletedTask;
        }

        var relFilePath = Path.GetRelativePath(SourceRoot, sourceFilePathArtifact.SourcePath);
        var targetFilePath = Path.GetFullPath(Path.Combine(TargetRoot, relFilePath));
        
        executor.Push(Out, executor.CreateCrate(crate, new TargetPathArtifact(targetFilePath)));

        return Task.CompletedTask;
    }
}