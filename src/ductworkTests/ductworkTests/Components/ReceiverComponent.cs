using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ductwork;
using ductwork.Components;

#nullable enable
namespace ductworkTests.Components
{
    public class ReceiverComponent : SingleInComponent<IObjectArtifact>
    {
        private readonly object _lock = new();
        private readonly List<object> _values = new();

        public ReceiverComponent()
        {
            Values = new ReadOnlyCollection<object>(_values);
        }

        public readonly ReadOnlyCollection<object> Values;
        
        protected override Task ExecuteIn(Graph graph, IObjectArtifact value, CancellationToken token)
        {
            lock (_lock)
            {
                _values.Add(value.Object);
            }
            
            return Task.CompletedTask;
        }
    }
}