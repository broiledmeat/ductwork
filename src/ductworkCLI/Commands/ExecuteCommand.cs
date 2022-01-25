using System.Linq;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using ductwork.Builders.Xml;
using ductwork.Executors;

namespace ductworkCLI.Commands;

[Command("execute")]
public class ExecuteCommand : ICommand
{
    [CommandParameter(0, Description = "Path of the XML graph to execute.")]
    public string Path { get; set; } = "";

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var token = console.RegisterCancellationHandler();
        var builder = XmlBuilder.LoadPath(Path);

        var exceptions = builder.Validate().ToArray();

        if (exceptions.Any())
        {
            foreach (var exception in exceptions)
            {
                await console.Error.WriteLineAsync(exception.ToString());
            }
            
            return;
        }

        var executor = builder.GetGraph().GetExecutor<ThreadedExecutor>();
        await executor.Execute(token);
    }
}