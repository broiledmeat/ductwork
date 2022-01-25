using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Resources;
using ductwork.TaskRunners;
using NLog;

#nullable enable
namespace ductwork.Executors;

public interface IExecutor
{
    string DisplayName { get; }
    Logger Log { get; }
    TaskRunner Runner { get; }
    Task Execute(CancellationToken token);
    Task Push(OutputPlug output, IArtifact artifact);
    Task<IArtifact> Get(InputPlug input, CancellationToken token);
    int Count(InputPlug input);
    bool IsFinished(InputPlug input);
    T GetResource<T>() where T : IResource;
}