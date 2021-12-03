using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;

#nullable enable
namespace ductwork.Components;

public class DirectoryFilePathIteratorComponent : SingleOutComponent<FilePathArtifact>
{
    public readonly string Path;
    public readonly bool IsRecursive;
    public readonly bool IncludeHidden;

    public DirectoryFilePathIteratorComponent(string path, bool isRecursive = true, bool includeHidden = false)
    {
        Path = System.IO.Path.GetFullPath(path);
        IsRecursive = isRecursive;
        IncludeHidden = includeHidden;
    }

    public override async Task Execute(Graph graph, CancellationToken token)
    {
        var options = IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var path in Directory.EnumerateFiles(Path, "*.*", options))
        {
            token.ThrowIfCancellationRequested();

            if (!IncludeHidden && IsPathOrParentsHidden(path))
            {
                continue;
            }

            var artifact = new FilePathArtifact(path);
            await graph.Push(Out, artifact);
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