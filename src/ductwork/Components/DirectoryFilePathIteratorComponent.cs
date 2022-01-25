using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Executors;

#nullable enable
namespace ductwork.Components;

public class DirectoryFilePathIteratorComponent : SingleOutComponent
{
    public Setting<string> Path = new();
    public Setting<bool> IsRecursive = new(true);
    public Setting<bool> IncludeHidden = new(false);

    public override async Task Execute(IExecutor executor, CancellationToken token)
    {
        var fullPath = System.IO.Path.GetFullPath(Path);
        var options = IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var path in Directory.EnumerateFiles(fullPath, "*.*", options))
        {
            token.ThrowIfCancellationRequested();

            if (!IncludeHidden.Value && IsPathOrParentsHidden(path))
            {
                continue;
            }

            var artifact = new FilePathArtifact(path);
            await executor.Push(Out, artifact);
        }
    }

    private bool IsPathOrParentsHidden(string path)
    {
        while (!System.IO.Path.GetFullPath(path).Equals(Path))
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.Hidden))
            {
                return true;
            }

            if (System.IO.Path.GetDirectoryName(path) is { } parentPath)
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