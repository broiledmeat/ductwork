using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Crates;
using ductwork.Executors;
using GlobExpressions;

namespace ductwork.Components;

public class SourcePathGlobMatchComponent : SingleInComponent
{
    public readonly OutputPlug True = new();
    public readonly OutputPlug False = new();

    public Setting<string> Glob = string.Empty;

    private Glob? _glob;

    protected override async Task ExecuteIn(IExecutor executor, ICrate crate, CancellationToken token)
    {
        _glob ??= new Glob(Glob);

        if (crate.Get<ISourcePathArtifact>() is not { } sourceFilePathArtifact)
        {
            return;
        }

        var output = _glob.IsMatch(sourceFilePathArtifact.SourcePath) ? True : False;
        await executor.Push(output, crate);
    }
}