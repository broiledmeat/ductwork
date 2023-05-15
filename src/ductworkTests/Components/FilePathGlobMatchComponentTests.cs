using System.Collections.Generic;
using System.Linq;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Crates;
using ductworkTests.TestHelpers;
using NUnit.Framework;

namespace ductworkTests.Components;

public class FilePathGlobMatchComponentTests
{
    private static void PushPaths(
        ComponentHarness graph,
        SourcePathGlobMatchComponent component,
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
            .OfType<ISourcePathArtifact>()
            .Select(artifact => artifact.SourcePath)
            .ToHashSet();
        return expectedPaths.ToHashSet().SetEquals(actualPaths);
    }

    [Test]
    public void ExecutesWithExpectedOutput_MatchRecursive()
    {
        var paths = new HashSet<string>
        {
            "childA.txt",
            "parent/childA.txt",
            "parent/childB.barp",
            "parent/sub/childA.tnt",
            "parent/sub/childB.tart",
        };

        var component = new SourcePathGlobMatchComponent {Glob = "parent/**/*.t?t"};
        var harness = new ComponentHarness(component);

        PushPaths(harness, component, paths);

        var outputs = harness.Execute();

        Assert.That(ArePathSetsEqual(new[]
            {
                "parent/childA.txt",
                "parent/sub/childA.tnt",
            },
            outputs[component.True]));
        Assert.That(ArePathSetsEqual(new[]
            {
                "childA.txt",
                "parent/childB.barp",
                "parent/sub/childB.tart",
            },
            outputs[component.False]));
    }

    [Test]
    public void ExecutesWithExpectedOutput_MatchNonRecursive()
    {
        var paths = new HashSet<string>
        {
            "childA.txt",
            "parent/childA.txt",
            "parent/childB.barp",
            "parent/sub/childA.tnt",
            "parent/sub/childB.tart",
        };

        var component = new SourcePathGlobMatchComponent {Glob = "parent/*.t?t"};
        var harness = new ComponentHarness(component);

        PushPaths(harness, component, paths);

        var outputs = harness.Execute();

        Assert.That(ArePathSetsEqual(new[]
            {
                "parent/childA.txt",
            },
            outputs[component.True]));
        Assert.That(ArePathSetsEqual(new[]
            {
                "childA.txt",
                "parent/childB.barp",
                "parent/sub/childA.tnt",
                "parent/sub/childB.tart",
            },
            outputs[component.False]));
    }
}