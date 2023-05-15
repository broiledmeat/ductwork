using ductwork.Artifacts;

namespace ductwork.Crates;

public interface ICrate
{
    T? Get<T>() where T : IArtifact;
    IArtifact[] GetAll();
}