using ductwork.Artifacts;

namespace ductworkTests.TestHelpers;

public class ObjectArtifact : Artifact
{
    public ObjectArtifact(object obj)
    {
        Object = obj;
    }

    public object Object { get; }
}