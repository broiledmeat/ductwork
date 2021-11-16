using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork.Artifacts
{
    public class CopyFileArtifact : IFinalizingArtifact, IFilePathAndTargetFilePathArtifact
    {
        public string FilePath { get; }
        public string TargetFilePath { get; }
        
        public CopyFileArtifact(string sourcePath, string targetPath)
        {
            FilePath = sourcePath;
            TargetFilePath = targetPath;
            
            var info = new FileInfo(FilePath);
            ContentId = $"{info.Length};{info.LastWriteTimeUtc}";
        }

        public string Id => TargetFilePath;

        public string ContentId { get; }

        public bool RequiresFinalize()
        {
            if (!File.Exists(TargetFilePath))
            {
                return true;
            }
            
            var sourceInfo = new FileInfo(FilePath);
            var targetInfo = new FileInfo(TargetFilePath);

            return sourceInfo.Length != targetInfo.Length ||
                   sourceInfo.LastWriteTimeUtc != targetInfo.LastWriteTimeUtc;
        }

        public async Task<bool> Finalize(CancellationToken token)
        {
            var dir = Path.GetDirectoryName(TargetFilePath);
            
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }
            
            var sourceInfo = new FileInfo(FilePath);

            File.Copy(FilePath, TargetFilePath, true);
            File.SetLastWriteTimeUtc(TargetFilePath, sourceInfo.LastWriteTimeUtc);
            return await Task.FromResult(true);
        }
    }
}