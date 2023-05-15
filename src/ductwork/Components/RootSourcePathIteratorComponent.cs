using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Crates;
using ductwork.Executors;

#nullable enable
namespace ductwork.Components;

public class RootSourcePathIteratorComponent : SingleOutComponent
{
    public Setting<string> SourceRoot = string.Empty;
    public Setting<bool> IsRecursive = true;
    public Setting<bool> IncludeHidden = false;

    public override async Task Execute(IExecutor executor, CancellationToken token)
    {
        var fullPath = Path.GetFullPath(SourceRoot);
        var options = IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var path in Directory.EnumerateFiles(fullPath, "*.*", options))
        {
            token.ThrowIfCancellationRequested();

            if (!IncludeHidden.Value && IsPathOrParentsHidden(path))
            {
                continue;
            }

            var crate = executor.CreateCrate(new SourcePathArtifact(path));
            await executor.Push(Out, crate);
        }
    }

    private bool IsPathOrParentsHidden(string path)
    {
        while (!Path.GetFullPath(path).Equals(SourceRoot))
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.Hidden))
            {
                return true;
            }

            if (Path.GetDirectoryName(path) is { } parentPath)
            {
                path = parentPath;
            }
            else
            {
                return false;
            }
        }

        return false;
    }
}