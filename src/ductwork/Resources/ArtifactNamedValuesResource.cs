using System.Collections.Generic;
using System.Linq;
using ductwork.Artifacts;
using GlobExpressions;

#nullable enable
namespace ductwork.Resources;

public class ArtifactNamedValuesResource : IResource
{
    public record NamedValue(IFilePathArtifact Artifact, string Name, object? Value);

    private readonly object _lock = new();
    private readonly HashSet<NamedValue> _values = new();

    public NamedValue[] Get(string name)
    {
        return _values.Where(item => item.Name == name).ToArray();
    }

    public NamedValue[] Get(string name, object? value)
    {
        return _values
            .Where(item =>
                item.Name == name &&
                item.Value?.Equals(value) == true)
            .ToArray();
    }

    public NamedValue[] GetGlob(string name, string value)
    {
        var valueGlob = new Glob(value);
        return _values
            .Where(item =>
                item.Name == name &&
                item.Value is string stringValue && valueGlob.IsMatch(stringValue))
            .ToArray();
    }

    public NamedValue[] Get(IFilePathArtifact artifact)
    {
        return _values.Where(item => item.Artifact == artifact).ToArray();
    }

    public void Set(IFilePathArtifact artifact, string name, object value)
    {
        lock (_lock)
        {
            if (!_values.Any(item => item.Artifact == artifact && item.Name == name))
            {
                _values.Add(new NamedValue(artifact, name, value));
            }
        }
    }
}