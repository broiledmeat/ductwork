using System;

#nullable enable
namespace ductwork
{
    internal interface IInputPlug
    {
        Type Type { get; }
    }
    
    public class InputPlug<T> : IInputPlug
    {
        public Type Type => typeof(T);
    }
}