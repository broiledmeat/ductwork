using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Crates;
using ductwork.Executors;
using ductwork.Resources;
using Scriban;
using Scriban.Syntax;

namespace ductworkScriban.Components;

public class TemplateParserComponent : InputAwaiterComponent
{
    private const string SetContextName = "set_context";

    public Setting<string> SourceRoot = string.Empty;

    protected override async Task ExecuteIn(IExecutor executor, ICrate crate, CancellationToken token)
    {
        if (crate.Get<ISourcePathArtifact>() is not { } sourceFilePathArtifact)
        {
            return;
        }
        
        var resource = executor.GetResource<NamedValuesResource>();

        var relPath = Path.GetRelativePath(SourceRoot, sourceFilePathArtifact.SourcePath);
        resource.Set(sourceFilePathArtifact.SourcePath, "_relPath", relPath);

        try
        {
            var template = Template.Parse(await File.ReadAllTextAsync(sourceFilePathArtifact.SourcePath, token));

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

                resource.Set(sourceFilePathArtifact.SourcePath, nameArg, args[1]);
            }

            await base.ExecuteIn(executor, crate, token);
        }
        catch (Exception e)
        {
            executor.Log.Error(e, $"Exception parsing {sourceFilePathArtifact.SourcePath}: {e.Message}");
        }
    }
}