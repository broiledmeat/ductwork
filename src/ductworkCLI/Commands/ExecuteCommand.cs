using System.Threading;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using ductwork.Executors;
using ductwork.FileLoaders;

namespace ductworkCLI.Commands;

[Command("execute")]
public class ExecuteCommand : ICommand
{
    [CommandParameter(0, Description = "Path of the XML graph to execute.")]
    public string Path { get; set; } = "";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var graph = GraphXmlLoader.LoadPath(Path);
        var executor = graph.GetExecutor<ThreadedExecutor>();
        await executor.Execute(CancellationToken.None);
    }
}