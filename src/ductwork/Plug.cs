using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork
{
    public interface IPlug
    {
        Type Type { get; }
        void SetFinished();
    }
    
    public class Plug<T> : IPlug
    {
        private readonly AsyncQueue<T> _values = new();
        private bool _finished;

        public Type Type => typeof(T);
        public int Count => _values.Count;
        public bool IsFinished => Count == 0 && _finished;

        public void SetFinished()
        {
            _finished = true;
        }

        public async Task<bool> Add(T value)
        {
            return await _values.Enqueue(value);
        }

        public async Task<T> Get(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                
                if (_values.Count == 0)
                {
                    await Task.Delay(50, token);
                    continue;
                }

                return await _values.Dequeue(token);
            }
        }
        
        public override string ToString()
        {
            return $"{GetType().Name}<{Type.Name}>()";
        }
    }
}