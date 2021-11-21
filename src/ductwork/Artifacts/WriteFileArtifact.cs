using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Force.Crc32;

#nullable enable
namespace ductwork.Artifacts;

public class WriteFileArtifact : IFinalizingArtifact, ITargetFilePathArtifact
{
    public readonly byte[] Content;
    public string TargetFilePath { get; }

    public WriteFileArtifact(byte[] content, string targetPath)
    {
        Content = content;
        TargetFilePath = targetPath;

        ContentId = Crc32Algorithm.Compute(Content).ToString();
    }

    public string Id => TargetFilePath;
        
    public string ContentId { get; }
        
    public bool RequiresFinalize()
    {
        if (!File.Exists(TargetFilePath))
        {
            return true;
        }
            
        var targetInfo = new FileInfo(TargetFilePath);

        return Content.Length != targetInfo.Length;
    }

    public async Task<bool> Finalize(CancellationToken token)
    {
        var dir = Path.GetDirectoryName(TargetFilePath);

        if (dir != null)
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllBytesAsync(TargetFilePath, Content, token);
        return await Task.FromResult(true);
    }
}