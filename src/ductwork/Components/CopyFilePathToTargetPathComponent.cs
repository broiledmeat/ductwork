using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Executors;

#nullable enable
namespace ductwork.Components;

public class CopyFilePathToTargetPathComponent : SingleInSingleOutComponent
{
    public Setting<string> SourceRoot = new();
    public Setting<string> TargetRoot = new();

    private string? _fullSourceRoot;
    private string? _fullTargetRoot;

    protected override async Task ExecuteIn(IExecutor executor, IArtifact artifact, CancellationToken token)
    {
        _fullSourceRoot ??= Path.GetFullPath(SourceRoot);
        _fullTargetRoot ??= Path.GetFullPath(TargetRoot);

        if (artifact is not IFilePathArtifact filePathArtifact)
        {
            return;
        }

        var targetPath = Path.Combine(
            _fullTargetRoot,
            Path.GetRelativePath(_fullSourceRoot, filePathArtifact.FilePath));
        var copyFileArtifact = new CopyFileArtifact(filePathArtifact.FilePath, targetPath);
        await executor.Push(Out, copyFileArtifact);
    }
}