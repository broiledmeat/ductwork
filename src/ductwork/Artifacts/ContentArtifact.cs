using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork.Artifacts;

public class ContentArtifact : Artifact, IContentArtifact
{
    private readonly byte[] _content;

    public ContentArtifact(byte[] content)
    {
        _content = content;
    }
    
    public Task<byte[]> GetContent(CancellationToken token)
    {
        return Task.FromResult(_content);
    }

    public override string ToString()
    {
        return $"{GetType().Name}{_content.Length / 1024}kb)";
    }
}