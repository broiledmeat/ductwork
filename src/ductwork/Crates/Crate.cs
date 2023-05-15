using System.Linq;
using ductwork.Artifacts;

namespace ductwork.Crates;

public class Crate : ICrate
{
    private readonly IArtifact[] _artifacts;

    public Crate(params IArtifact[] artifacts)
    {
        _artifacts = artifacts;
    }

    public Crate(ICrate baseCrate, params IArtifact[] artifacts)
    {
        _artifacts = baseCrate.GetAll()
            .Concat(artifacts)
            .ToArray();
    }

    public T? Get<T>() where T : IArtifact
    {
        return _artifacts
            .OfType<T>()
            .LastOrDefault();
    }

    public IArtifact[] GetAll()
    {
        return _artifacts;
    }

    public override string ToString()
    {
        var artifactNames = string.Join(", ", _artifacts.Select(artifact => artifact.GetType().Name));
        return $"{GetType().Name}({artifactNames})";
    }
}