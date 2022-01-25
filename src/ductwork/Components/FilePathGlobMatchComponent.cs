using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Executors;
using GlobExpressions;

#nullable enable
namespace ductwork.Components;

public class FilePathGlobMatchComponent : SingleInComponent
{
    public readonly OutputPlug True = new();
    public readonly OutputPlug False = new();
        
    public Setting<string> Glob = new();

    private Glob? _glob;

    protected override async Task ExecuteIn(IExecutor executor, IArtifact artifact, CancellationToken token)
    {
        _glob ??= new Glob(Glob);
        
        if (artifact is not IFilePathArtifact filePathArtifact)
        {
            return;
        }
        
        var output = _glob.IsMatch(filePathArtifact.FilePath) ? True : False;
        var matchingArtifact = new FilePathArtifact(filePathArtifact.FilePath);
        await executor.Push(output, matchingArtifact);
    }
}