#nullable enable
namespace ductwork.Artifacts;

public class TargetPathArtifact : Artifact, ITargetPathArtifact
{
    public TargetPathArtifact(string targetPath)
    {
        TargetPath = targetPath;
    }
    
    public string TargetPath { get; }

    public override string ToString()
    {
        return $"{GetType().Name}({TargetPath})";
    }
}