using ductwork.Artifacts;

#nullable enable
namespace ductworkTests.TestHelpers;

public class ObjectArtifact : Artifact
{
    public ObjectArtifact(object obj)
    {
        Id = obj.ToString() ?? string.Empty;
        Object = obj;
    }
    
    public object Object { get; }
}