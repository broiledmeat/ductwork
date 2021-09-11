using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

#nullable enable
namespace ductwork
{
    public class AsyncQueue<T> : IAsyncEnumerable<T>
    {
        private readonly SemaphoreSlim _enumerationSemaphore = new(1);
        private readonly BufferBlock<T> _bufferBlock = new();

        public int Count => _bufferBlock.Count;

        public async Task<bool> Enqueue(T item) => await _bufferBlock.SendAsync(item);

        public async Task<T> Dequeue(CancellationToken token = default)
        {
            return await _bufferBlock.ReceiveAsync(token);
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default)
        {
            await _enumerationSemaphore.WaitAsync(token);
            
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    yield return await _bufferBlock.ReceiveAsync(token);
                }
            }
            finally
            {
                _enumerationSemaphore.Release();
            }
        }
    }
}