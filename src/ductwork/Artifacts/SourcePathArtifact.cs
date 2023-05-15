using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ductwork.Artifacts;

public class SourcePathArtifact : Artifact, ISourcePathArtifact
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private byte[]? _cachedContent;

    public SourcePathArtifact(string sourcePath)
    {
        SourcePath = sourcePath;
    }

    public string SourcePath { get; }

    public async Task<byte[]> GetContent(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);

        try
        {
            return _cachedContent ??= await File.ReadAllBytesAsync(SourcePath, token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override string ToString()
    {
        return $"{GetType().Name}({SourcePath})";
    }
}