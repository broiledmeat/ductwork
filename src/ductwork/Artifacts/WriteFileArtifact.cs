using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Force.Crc32;

#nullable enable
namespace ductwork.Artifacts
{
    public class WriteFileArtifact : Artifact
    {
        public readonly byte[] Content;
        public readonly string TargetPath;

        public WriteFileArtifact(byte[] content, string targetPath)
        {
            Content = content;
            TargetPath = targetPath;

            ContentId = Crc32Algorithm.Compute(Content).ToString();
        }

        public override string Id => TargetPath;
        
        public override string ContentId { get; }
        
        public override bool RequiresFinalize()
        {
            if (!File.Exists(TargetPath))
            {
                return true;
            }
            
            var targetInfo = new FileInfo(TargetPath);

            return Content.Length != targetInfo.Length;
        }

        public override async Task<bool> Finalize(CancellationToken token)
        {
            var dir = Path.GetDirectoryName(TargetPath);

            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllBytesAsync(TargetPath, Content, token);
            return await Task.FromResult(true);
        }
    }
}