using System;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using ductwork.Components;
using ductwork.FileLoaders;

namespace ductworkCLI.Commands
{
    [Command("build")]
    public class BuildCommand : ICommand
    {
        [CommandParameter(0, Description = "Path of the XML graph to load.")]
        public string Path { get; set; } = "";
        
        public async ValueTask ExecuteAsync(IConsole console)
        {
            FinalizeArtifactsComponent.ArtifactFinalized += FinalizeArtifactsComponentOnArtifactFinalized;

            try
            {
                var graph = GraphXmlLoader.LoadPath(Path);
                await graph.Execute();
            }
            finally
            {
                FinalizeArtifactsComponent.ArtifactFinalized -= FinalizeArtifactsComponentOnArtifactFinalized;
            }
        }

        private static void FinalizeArtifactsComponentOnArtifactFinalized(FinalizeArtifactsComponent sender, FinalizedResult result)
        {
            Console.WriteLine($"[{sender}] Finalized {result.Artifact}; {result.State}; {result.Exception}");
        }
    }
}