using System.Collections.Generic;
using System.Linq;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;
using NUnit.Framework;

namespace ductworkTests.ComponentTests;

public class FilePathGlobMatchComponentTests
{
    private static void PushPaths(
        ComponentHarness graph,
        FilePathGlobMatchComponent component,
        IEnumerable<string> paths)
    {
        paths
            .Select(path => new FilePathArtifact(path))
            .ForEach(artifact => graph.QueuePush(component.In, artifact));
    }

    private static bool ArePathSetsEqual(IEnumerable<string> expectedPaths, IEnumerable<IArtifact> actualArtifacts)
    {
        var actualPaths = actualArtifacts
            .OfType<FilePathArtifact>()
            .Select(artifact => artifact.FilePath)
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

        var component = new FilePathGlobMatchComponent {Glob = "parent/**/*.t?t"};
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

        var component = new FilePathGlobMatchComponent {Glob = "parent/*.t?t"};
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