namespace ductwork.Artifacts;

public record TargetPathArtifact(string TargetPath) : Artifact, ITargetPathArtifact
{
    public override string ToString()
    {
        return $"{GetType().Name}({TargetPath})";
    }
}