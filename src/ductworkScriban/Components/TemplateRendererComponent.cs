using System.Text;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Resources;
using Scriban;
using Scriban.Functions;
using Scriban.Parsing;
using Scriban.Runtime;

namespace ductworkScriban.Components;

public class TemplateRendererComponent : SingleInSingleOutComponent<IFilePathArtifact, WriteFileArtifact>
{
    public readonly string SourceRoot;
    public readonly string TargetRoot;

    private ArtifactNamedValuesResource? _resource;
    private TemplateLoader? _templateLoader;

    public TemplateRendererComponent(string sourceRoot, string targetRoot)
    {
        SourceRoot = sourceRoot;
        TargetRoot = targetRoot;
    }

    protected override async Task ExecuteIn(Graph graph, IFilePathArtifact value, CancellationToken token)
    {
        _resource ??= graph.GetResource<ArtifactNamedValuesResource>();
        _templateLoader ??= new TemplateLoader(SourceRoot);

        var contextVars = _resource.Get(value);
        var disableRender = contextVars.Any(contextVar => 
            contextVar.Name == "rendererEnable" && contextVar.Value is false);

        if (disableRender)
        {
            return;
        }

        var targetPath = Path.Combine(TargetRoot, Path.GetRelativePath(SourceRoot, value.FilePath));
        var script = new BuiltinFunctions();

        _resource.Get(value).ForEach(contextVar => script.Add(contextVar.Name, contextVar.Value));
        script.Import("set_context", SetContextFunc);
        script.Import("get_contexts", (string n, object v) => GetContextsFunc(_resource, n, v));

        var context = new TemplateContext(script) {TemplateLoader = _templateLoader};

        try
        {
            var template = Template.Parse(await File.ReadAllTextAsync(value.FilePath, token));
            var content = await template.RenderAsync(context) ?? string.Empty;
            var artifact = new WriteFileArtifact(Encoding.UTF8.GetBytes(content), targetPath);
            await graph.Push(Out, artifact);
        }
        catch (Exception e)
        {
            graph.Log.Error(e, $"Exception rendering {value.FilePath}: {e.Message}");
        }
    }

    private static void SetContextFunc(string name, object value)
    {
    }

    private IEnumerable<Dictionary<string, object?>> GetContextsFunc(
        ArtifactNamedValuesResource resource,
        string name,
        object value)
    {
        var artifacts = (value is string strValue
                ? resource.GetGlob(name, strValue)
                : resource.Get(name, value))
            .Select(contextVar => contextVar.Artifact)
            .Distinct();

        foreach (var artifact in artifacts)
        {
            var contextVars = resource
                .Get(artifact)
                .ToDictionary(contextVar => contextVar.Name, contextVar => contextVar.Value);
            
            contextVars.Add("_artifact", artifact);
            contextVars.Add("_relPath", Path.GetRelativePath(SourceRoot, artifact.FilePath));

            yield return contextVars;
        }
    }

    private class TemplateLoader : ITemplateLoader
    {
        private readonly string _sourceRoot;

        public TemplateLoader(string sourceRoot)
        {
            _sourceRoot = sourceRoot;
        }

        public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
        {
            return Path.Join(_sourceRoot, templateName);
        }

        public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            return File.ReadAllText(templatePath);
        }

        public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            return await File.ReadAllTextAsync(templatePath);
        }
    }
}