#nullable enable
namespace ductwork.Artifacts;

public class FilePathArtifact : Artifact, IFilePathArtifact
{
    public FilePathArtifact(string filePath)
    {
        FilePath = filePath;
        Id = filePath;
    }
    
    public string FilePath { get; }

    public override string ToString()
    {
        return $"{GetType().Name}({FilePath})";
    }
}