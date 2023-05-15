using System.Threading;
using System.Threading.Tasks;

namespace ductwork.Artifacts;

public interface IArtifact
{
}

public interface IContentArtifact : IArtifact
{
    Task<byte[]> GetContent(CancellationToken token);
}

public interface ISourcePathArtifact : IContentArtifact
{
    string SourcePath { get; }
}

public interface ITargetPathArtifact : IArtifact
{
    string TargetPath { get; }
}