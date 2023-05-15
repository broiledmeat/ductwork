using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Crates;
using ductwork.Executors;
using ductwork.Resources;
using ductworkScriban.Artifacts;

namespace ductworkScriban.Components;

public class TemplateRendererComponent : SingleInSingleOutComponent
{
    public Setting<string> SourceRoot = string.Empty;

    private NamedValuesResource? _resource;

    protected override async Task ExecuteIn(IExecutor executor, ICrate crate, CancellationToken token)
    {
        if (crate.Get<ISourcePathArtifact>() is not { } sourceFilePathArtifact)
        {
            return;
        }

        _resource ??= executor.GetResource<NamedValuesResource>();

        var contextVars = _resource.Get(sourceFilePathArtifact.SourcePath);
        var enableRender = !contextVars
            .Any(contextVar => contextVar is {Name: "rendererEnable", Value: false});

        if (!enableRender)
        {
            return;
        }

        await executor.Push(
            Out,
            executor.CreateCrate(
                crate,
                new TemplateSourceFileArtifact(_resource, SourceRoot, sourceFilePathArtifact.SourcePath)));
    }
}