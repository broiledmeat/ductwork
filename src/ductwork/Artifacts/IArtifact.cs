using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork.Artifacts;

public interface IArtifact
{
    string Id { get; }
    uint Checksum { get; }
}

public interface IFilePathArtifact : IArtifact
{
    string FilePath { get; }
}

public interface ITargetFilePathArtifact : IArtifact
{
    string TargetFilePath { get; }
}


public interface IFinalizingArtifact : IArtifact
{
    bool RequiresFinalize() => true;
    Task<bool> Finalize(CancellationToken token = default);
}