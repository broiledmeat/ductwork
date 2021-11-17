using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;

#nullable enable
namespace ductwork.Components
{
    public class CopyFilePathToTargetPathComponent : SingleInSingleOutComponent<IFilePathArtifact, CopyFileArtifact>
    {
        public readonly string SourceRoot;
        public readonly string TargetRoot;

        public CopyFilePathToTargetPathComponent(string sourceRoot, string targetRoot)
        {
            SourceRoot = sourceRoot;
            TargetRoot = targetRoot;
        }

        protected override async Task ExecuteIn(Graph graph, IFilePathArtifact value, CancellationToken token)
        {
            var targetPath = Path.Combine(TargetRoot, Path.GetRelativePath(SourceRoot, value.FilePath));
            var artifact = new CopyFileArtifact(value.FilePath, targetPath);
            await graph.Push(Out, artifact);
        }
    }
}