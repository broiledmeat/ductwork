using System;
using System.Collections.Generic;
using System.Linq;
using ductwork.Artifacts;

#nullable enable
namespace ductwork.Crates;

public class Crate : ICrate
{
    private IArtifact[] _artifacts = Array.Empty<IArtifact>();

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