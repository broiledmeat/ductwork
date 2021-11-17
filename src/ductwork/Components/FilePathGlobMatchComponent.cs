using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;

#nullable enable
namespace ductwork.Components
{
    public class FilePathGlobMatchComponent : SingleInComponent<IFilePathArtifact>
    {
        public readonly OutputPlug<FilePathArtifact> True = new();
        public readonly OutputPlug<FilePathArtifact> False = new();
        
        public readonly string Glob;
        public readonly Regex GlobRegex;

        public FilePathGlobMatchComponent(string glob)
        {
            Glob = glob;
            GlobRegex = GlobToRegex(glob);
        }

        protected override async Task ExecuteIn(Graph graph, IFilePathArtifact value, CancellationToken token)
        {
            var output = GlobRegex.IsMatch(value.FilePath) ? True : False;
            var artifact = new FilePathArtifact(value.FilePath);
            await graph.Push(output, artifact);
        }

        private static Regex GlobToRegex(string glob)
        {
            var pattern = new StringBuilder();
            for (var i = 0; i < glob.Length; i++)
            {
                var c = glob[i];

                switch (c)
                {
                    case '.':
                        pattern.Append(@"\.");
                        break;
                    case '?':
                        pattern.Append(@"[^/]");
                        break;
                    case '*':
                        if (i < glob.Length - 1 && glob[i + 1] == '*')
                        {
                            pattern.Append(@".*");
                            i += 1;
                        }
                        else
                        {
                            pattern.Append(@"[^/]*");
                        }
                        break;
                    default:
                        pattern.Append(c);
                        break;
                }
            }
            
            pattern.Append('$');

            return new Regex(pattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }
    }
}