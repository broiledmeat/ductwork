using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork.Artifacts;

public interface IArtifact
{
    string? ToString()
    {
        return $"{GetType().Name}()";
    }
}

public interface IFinalizingArtifact : IArtifact
{
    string Id { get; }
    string ContentId { get; }
    bool RequiresFinalize() => true;
    Task<bool> Finalize(CancellationToken token = default);

    new string? ToString()
    {
        return $"{GetType().Name}({Id}, {ContentId})";
    }
}