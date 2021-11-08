using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ductwork;

#nullable enable
namespace ductworkTests.Components
{
    public class ReceiverComponent<T> : SingleInComponent<T>
    {
        private readonly object _lock = new();
        private readonly List<T> _values = new();

        public ReceiverComponent()
        {
            Values = new ReadOnlyCollection<T>(_values);
        }

        public readonly ReadOnlyCollection<T> Values;
        
        public override Task ExecuteIn(Graph graph, T value, CancellationToken token)
        {
            lock (_lock)
            {
                _values.Add(value);
            }
            
            return Task.CompletedTask;
        }
    }
}