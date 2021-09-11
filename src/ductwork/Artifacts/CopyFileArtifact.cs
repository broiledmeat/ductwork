using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork.Artifacts
{
    public class CopyFileArtifact : Artifact
    {
        public readonly string SourcePath;
        public readonly string TargetPath;
        
        public CopyFileArtifact(string sourcePath, string targetPath)
        {
            SourcePath = sourcePath;
            TargetPath = targetPath;
            
            var info = new FileInfo(SourcePath);
            ContentId = $"{info.Length};{info.LastWriteTimeUtc}";
        }

        public override string Id => TargetPath;

        public override string ContentId { get; }

        public override bool RequiresFinalize()
        {
            if (!File.Exists(TargetPath))
            {
                return true;
            }
            
            var sourceInfo = new FileInfo(SourcePath);
            var targetInfo = new FileInfo(TargetPath);

            return sourceInfo.Length != targetInfo.Length ||
                   sourceInfo.LastWriteTimeUtc != targetInfo.LastWriteTimeUtc;
        }

        public override async Task<bool> Finalize(CancellationToken token)
        {
            var dir = Path.GetDirectoryName(TargetPath);
            
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }
            
            var sourceInfo = new FileInfo(SourcePath);

            File.Copy(SourcePath, TargetPath, true);
            File.SetLastWriteTimeUtc(TargetPath, sourceInfo.LastWriteTimeUtc);
            return await Task.FromResult(true);
        }
    }
}