using System.Collections.Generic;
using System.IO;
using System.Linq;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Crates;
using ductworkTests.TestHelpers;
using NUnit.Framework;

#nullable enable
namespace ductworkTests.Components;

public class TransformSourcePathToTargetPathComponentTests
{
    private static void PushPaths(
        ComponentHarness graph,
        TransformSourcePathToTargetPathComponent component,
        IEnumerable<string> paths)
    {
        paths
            .Select(path => graph.CreateCrate(new SourcePathArtifact(path)))
            .ForEach(crate => graph.QueuePush(component.In, crate));
    }

    private static bool ArePathSetsEqual(IEnumerable<string> expectedPaths, IEnumerable<ICrate> actualCrates)
    {
        var actualPaths = actualCrates
            .SelectMany(crate => crate.GetAll())
            .OfType<ITargetPathArtifact>()
            .Select(artifact => artifact.TargetPath)
            .ToHashSet();
        return expectedPaths.ToHashSet().SetEquals(actualPaths);
    }

    [Test]
    public void ExecutesWithExpectedOutput_RootTransform()
    {
        const string sourceRoot = "/ding/dong";
        const string targetRoot = "/ding/barp";
        var paths = new[]
        {
            "childA.txt",
            "parent/childA.txt",
            "parent/childB.barp",
            "parent/sub/childA.tnt",
            "parent/sub/childB.tart",
        };

        var sourcePaths = paths.Select(relPath => Path.Combine(sourceRoot, relPath)).ToHashSet();

        var component = new TransformSourcePathToTargetPathComponent()
        {
            SourceRoot = sourceRoot,
            TargetRoot = targetRoot,
        };
        var harness = new ComponentHarness(component);

        PushPaths(harness, component, sourcePaths);

        var outputs = harness.Execute();

        var targetPaths = paths.Select(relPath => Path.Combine(targetRoot, relPath)).ToHashSet();
        Assert.That(ArePathSetsEqual(targetPaths, outputs[component.Out]));
    }
}