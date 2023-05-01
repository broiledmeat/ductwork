using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Force.Crc32;

#nullable enable
namespace ductwork.Artifacts;

public class CopyFileArtifact : Artifact, IFilePathArtifact, ITargetFilePathArtifact, IFinalizingArtifact
{
    public CopyFileArtifact(string sourceFilePath, string targetFilePath)
    {
        FilePath = sourceFilePath;
        TargetFilePath = targetFilePath;

        var info = new FileInfo(FilePath);
        
        Id = targetFilePath;
        Checksum = CreateChecksum(new object[] { info.Length, info.LastWriteTimeUtc.ToBinary() });
    }

    public string FilePath { get; }

    public string TargetFilePath { get; }

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