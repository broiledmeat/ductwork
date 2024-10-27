using System.Threading;
using System.Threading.Tasks;

namespace ductwork.Artifacts;

public record ContentArtifact(byte[] Content) : Artifact, IContentArtifact
{
    public Task<byte[]> GetContent(CancellationToken token)
    {
        return Task.FromResult(Content);
    }

    public override string ToString()
    {
        return $"{GetType().Name}{Content.Length / 1024}kb)";
    }
}