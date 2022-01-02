using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable
namespace ductwork.Components;

public interface ISetting
{
    Type Type { get; }
    bool HasValue { get; }
    object Value { get; }
}

public readonly struct Setting<T> : ISetting
{
    private readonly T? _value;

    public Setting(T? defaultValue = default)
    {
        _value = defaultValue;
    }

    public Type Type => typeof(T);

    public bool HasValue => _value != null;

    public T Value => _value ?? throw new Exception("Value not set.");

    object ISetting.Value => Value!;
    
    public static implicit operator T(Setting<T> value) => value.Value;
    public static implicit operator Setting<T>(T value) => new(value);
}