using System;

#nullable enable
namespace ductwork
{
    public interface IOutputPlug
    {
        Type Type { get; }
    }

    public class OutputPlug<T> : IOutputPlug
    {
        public Type Type => typeof(T);
    }
}