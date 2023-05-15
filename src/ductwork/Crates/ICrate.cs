using ductwork.Artifacts;

#nullable enable
namespace ductwork.Crates;

public interface ICrate
{
    T? Get<T>() where T : IArtifact;
    IArtifact[] GetAll();
}