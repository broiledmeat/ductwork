using System.Threading;
using System.Threading.Tasks;
using ductwork.Executors;

namespace ductwork.Components;

public interface IComponent
{
    string DisplayName { get; }
    Task Execute(IExecutor executor, CancellationToken token);
}