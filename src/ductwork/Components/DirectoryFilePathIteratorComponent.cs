using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;

#nullable enable
namespace ductwork.Components
{
    public class DirectoryFilePathIteratorComponent : SingleOutComponent<FilePathArtifact>
    {
        public readonly string Path;
        public readonly bool IsRecursive;

        public DirectoryFilePathIteratorComponent(string path, bool isRecursive = true)
        {
            Path = path;
            IsRecursive = isRecursive;
        }

        public override async Task Execute(Graph graph, CancellationToken token)
        {
            var options = IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var path in Directory.EnumerateFiles(Path, "*.*", options))
            {
                token.ThrowIfCancellationRequested();
                var artifact = new FilePathArtifact(path);
                await graph.Push(Out, artifact);
            }
        }
    }
}