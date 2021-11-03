using System;

#nullable enable
namespace ductwork
{
    public interface IInputPlug
    {
        Type Type { get; }
    }
    
    public class InputPlug<T> : IInputPlug
    {
        public Type Type => typeof(T);
    }
}