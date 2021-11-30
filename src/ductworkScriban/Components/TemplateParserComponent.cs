using ductwork;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Resources;
using Scriban;
using Scriban.Syntax;

namespace ductworkScriban.Components;

public class TemplateParserComponent : SingleInSingleOutComponent<IFilePathArtifact, IFilePathArtifact>
{
    private const string SetContextName = "set_context";
    
    protected override async Task ExecuteIn(Graph graph, IFilePathArtifact value, CancellationToken token)
    {
        var resource = graph.GetResource<ArtifactNamedValuesResource>();

        try
        {
            var template = Template.Parse(await File.ReadAllTextAsync(value.FilePath, token));

            var setContextExpressionArgs = template.Page.Body.Statements
                .OfType<ScriptExpressionStatement>()
                .Select(statement => statement.Expression)
                .OfType<ScriptFunctionCall>()
                .Where(call => call.Target is ScriptVariableGlobal {Name: SetContextName})
                .Select(call => call.Arguments);

            foreach (var expressionArg in setContextExpressionArgs)
            {
                var args = expressionArg.Children
                    .OfType<ScriptLiteral>()
                    .Select(literal => literal.Value)
                    .ToArray();

                if (expressionArg.Count != 2 || args.Length != 2)
                {
                    throw new Exception($"`{SetContextName}` must have two literal arguments.");
                }

                if (args[0] is not string nameArg)
                {
                    throw new Exception($"`{SetContextName}` first argument must be a string.");
                }

                resource.Set(value, nameArg, args[1]);
            }

            await graph.Push(Out, value);
        }
        catch (Exception e)
        {
            graph.Log.Error(e, $"Exception parsing {value.FilePath}: {e.Message}");
        }
    }
}