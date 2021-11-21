using System;
using ductwork.Artifacts;

#nullable enable
namespace ductwork;

public interface IOutputPlug
{
    Type Type { get; }
}

public class OutputPlug<T> : IOutputPlug where T : IArtifact
{
    public Type Type => typeof(T);
}