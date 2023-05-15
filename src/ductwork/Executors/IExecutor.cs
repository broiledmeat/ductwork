using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Crates;
using ductwork.Resources;
using ductwork.TaskRunners;
using NLog;

namespace ductwork.Executors;

public interface IExecutor
{
    string DisplayName { get; }
    Logger Log { get; }
    TaskRunner Runner { get; }
    Task Execute(CancellationToken token);
    ICrate CreateCrate(params IArtifact[] artifacts);
    ICrate CreateCrate(ICrate baseCrate, params IArtifact[] artifacts);
    Task Push(OutputPlug output, ICrate crate);
    Task<ICrate> Get(InputPlug input, CancellationToken token);
    int Count(InputPlug input);
    bool IsFinished(InputPlug input);
    T GetResource<T>() where T : IResource;
}