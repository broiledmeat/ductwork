#nullable enable
namespace ductwork.Artifacts;

public interface IFilePathArtifact : IArtifact
{
    string FilePath { get; }
}

public interface ITargetFilePathArtifact : IArtifact
{
    string TargetFilePath { get; }
}

public interface IFilePathAndTargetFilePathArtifact : IFilePathArtifact, ITargetFilePathArtifact
{
}

public record FilePathArtifact(string FilePath) : IFilePathArtifact;