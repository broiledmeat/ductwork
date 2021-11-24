using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using GlobExpressions;

#nullable enable
namespace ductwork.Components;

public class FilePathGlobMatchComponent : SingleInComponent<IFilePathArtifact>
{
    public readonly OutputPlug<FilePathArtifact> True = new();
    public readonly OutputPlug<FilePathArtifact> False = new();
        
    public readonly Glob Glob;

    public FilePathGlobMatchComponent(string glob)
    {
        Glob = new Glob(glob);
    }

    protected override async Task ExecuteIn(Graph graph, IFilePathArtifact value, CancellationToken token)
    {
        var output = Glob.IsMatch(value.FilePath) ? True : False;
        var artifact = new FilePathArtifact(value.FilePath);
        await graph.Push(output, artifact);
    }
}