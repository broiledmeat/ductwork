using System.Text;
using System.Text.RegularExpressions;
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
        
    public readonly Glob Glob;

    public FilePathGlobMatchComponent(string glob)
    {
        Glob = new Glob(glob);
    }

    protected override async Task ExecuteIn(GraphExecutor executor, IArtifact artifact, CancellationToken token)
    {
        if (artifact is not IFilePathArtifact filePathArtifact)
        {
            return;
        }
        
        var output = Glob.IsMatch(filePathArtifact.FilePath) ? True : False;
        var matchingArtifact = new FilePathArtifact(filePathArtifact.FilePath);
        await executor.Push(output, matchingArtifact);
    }
}