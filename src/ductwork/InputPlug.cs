using System;
using ductwork.Artifacts;

#nullable enable
namespace ductwork;

public interface IInputPlug
{
    Type Type { get; }
}

public class InputPlug<T> : IInputPlug where T : IArtifact
{
    public Type Type => typeof(T);
}