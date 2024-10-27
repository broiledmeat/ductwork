using System;

namespace ductwork.Components;

public interface ISetting
{
    Type Type { get; }
    bool HasValue { get; }
    object Value { get; }
}

public readonly struct Setting<T>(T? DefaultValue = default) : ISetting
{
    public Type Type => typeof(T);

    public bool HasValue => DefaultValue != null;

    public T Value => DefaultValue ?? throw new Exception($"{GetType().Name}<{typeof(T).Name}> value is not set.");

    object ISetting.Value => Value!;

    public static implicit operator T(Setting<T> value) => value.Value;
    public static implicit operator Setting<T>(T value) => new(value);
}