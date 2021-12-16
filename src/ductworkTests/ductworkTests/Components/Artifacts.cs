using ductwork.Artifacts;

#nullable enable
namespace ductworkTests.Components;

public interface IObjectArtifact : IArtifact
{
    object Object { get; }
}

public record ObjectArtifact(object Object) : IObjectArtifact;

public record StringArtifact(string Value) : ObjectArtifact(Value);

public record IntArtifact(int Value) : ObjectArtifact(Value);