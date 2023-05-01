using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Force.Crc32;

#nullable enable
namespace ductwork.Artifacts;

public class WriteFileArtifact : Artifact, ITargetFilePathArtifact, IFinalizingArtifact
{
    private readonly byte[] _content;

    public WriteFileArtifact(byte[] content, string targetPath)
    {
        _content = content;

        TargetFilePath = targetPath;

        Id = targetPath;
        Checksum = CreateChecksum(new object[] {_content});
    }

    public string TargetFilePath { get; }

    public bool RequiresFinalize()
    {
        if (!File.Exists(TargetFilePath))
        {
            return true;
        }

        var targetInfo = new FileInfo(TargetFilePath);

        return _content.Length != targetInfo.Length;
    }

    public async Task<bool> Finalize(CancellationToken token)
    {
        var dir = Path.GetDirectoryName(TargetFilePath);

        if (dir != null)
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllBytesAsync(TargetFilePath, _content, token);
        return await Task.FromResult(true);
    }
}