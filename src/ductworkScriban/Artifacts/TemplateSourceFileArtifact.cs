using System.Text;
using ductwork;
using ductwork.Artifacts;
using ductwork.Resources;
using Scriban;
using Scriban.Functions;
using Scriban.Parsing;
using Scriban.Runtime;

namespace ductworkScriban.Artifacts;

public record TemplateSourceFileArtifact : Artifact, ISourcePathArtifact
{
    private const string SetContextName = "set_context";
    private const string GetContextsName = "get_contexts";

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly NamedValuesResource _resource;
    private readonly string _templateRoot;
    private TemplateLoader? _templateLoader;
    private byte[]? _cachedContent;

    public TemplateSourceFileArtifact(NamedValuesResource resource, string templateRoot, string sourceFilePath)
    {
        _resource = resource;
        _templateRoot = templateRoot;
        SourcePath = sourceFilePath;
    }

    public string SourcePath { get; }

    public async Task<byte[]> GetContent(CancellationToken token)
    {
        await _semaphore.WaitAsync(token);

        try
        {
            if (_cachedContent != null)
            {
                return _cachedContent;
            }

            _templateLoader ??= new TemplateLoader(_templateRoot);

            var script = new BuiltinFunctions();

            _resource.Get(SourcePath).ForEach(contextVar => script.Add(contextVar.Name, contextVar.Value));
            script.Import(SetContextName, SetContextFunc);
            script.Import(GetContextsName, (string n, object v) => GetContextsFunc(_resource, n, v));

            var context = new TemplateContext(script) {TemplateLoader = _templateLoader};
            var template = Template.Parse(await File.ReadAllTextAsync(SourcePath, token));
            var content = await template.RenderAsync(context) ?? string.Empty;

            _cachedContent = Encoding.UTF8.GetBytes(content);
            return _cachedContent;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static void SetContextFunc(string name, object value)
    {
    }

    private static IEnumerable<Dictionary<string, object?>> GetContextsFunc(
        NamedValuesResource resource,
        string name,
        object value)
    {
        var contexts = (value is string strValue
                ? resource.GetGlob(name, strValue)
                : resource.Get(name, value))
            .Select(contextVar => contextVar.Context)
            .Distinct();

        foreach (var context in contexts)
        {
            yield return resource
                .GetAllForContext(context)
                .ToDictionary(contextVar => contextVar.Name, contextVar => contextVar.Value);
        }
    }

    private class TemplateLoader(string TemplateRoot) : ITemplateLoader
    {
        public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
        {
            return Path.Join(TemplateRoot, templateName);
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