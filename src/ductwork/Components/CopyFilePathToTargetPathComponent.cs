using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Executors;

#nullable enable
namespace ductwork.Components;

public class CopyFilePathToTargetPathComponent : SingleInSingleOutComponent
{
    public readonly string SourceRoot;
    public readonly string TargetRoot;

    public CopyFilePathToTargetPathComponent(string sourceRoot, string targetRoot)
    {
        SourceRoot = sourceRoot;
        TargetRoot = targetRoot;
    }

    protected override async Task ExecuteIn(GraphExecutor graph, IArtifact artifact, CancellationToken token)
    {
        if (artifact is not IFilePathArtifact filePathArtifact)
        {
            return;
        }
        
        var targetPath = Path.Combine(TargetRoot, Path.GetRelativePath(SourceRoot, filePathArtifact.FilePath));
        var copyFileArtifact = new CopyFileArtifact(filePathArtifact.FilePath, targetPath);
        await graph.Push(Out, copyFileArtifact);
    }
}