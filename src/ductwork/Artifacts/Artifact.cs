using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ductwork.Artifacts
{
    public abstract class Artifact
    {
        public abstract string Id { get; }
        
        public abstract string ContentId { get; }

        public virtual bool RequiresFinalize()
        {
            return true;
        }
        
        public abstract Task<bool> Finalize(CancellationToken token);

        public override string ToString()
        {
            return $"{GetType().Name}({Id}, {ContentId})";
        }
    }
}