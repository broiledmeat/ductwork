using System;

#nullable enable
namespace ductwork
{
    internal interface IOutputPlug
    {
        Type Type { get; }
    }

    public class OutputPlug<T> : IOutputPlug
    {
        public Type Type => typeof(T);
    }
}