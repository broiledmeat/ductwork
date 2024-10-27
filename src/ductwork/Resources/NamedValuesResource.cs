using System.Collections.Generic;
using System.Linq;
using GlobExpressions;

namespace ductwork.Resources;

public class NamedValuesResource : IResource
{
    private readonly object _lock = new();
    private readonly HashSet<NamedValue> _values = [];

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

    public NamedValue[] GetAllForContext(string context)
    {
        return _values.Where(item => item.Context == context).ToArray();
    }

    public void Set(string context, string name, object value)
    {
        lock (_lock)
        {
            if (!_values.Any(item => item.Context == context && item.Name == name))
            {
                _values.Add(new NamedValue(context, name, value));
            }
        }
    }

    public record NamedValue(string Context, string Name, object? Value);
}