using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ductwork.Artifacts;
using ductwork.Components;
using NUnit.Framework;

namespace ductworkTests.ComponentTests;

public class DirectoryFilePathIteratorComponentTests
{
    [Test]
    public void ExecutesWithExpectedOutput()
    {
        var expectedFilePaths = new HashSet<string>();

        var tempDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        for (var i = 0; i < 5; i++)
        {
            var tempFile = Path.Join(tempDir, Guid.NewGuid().ToString());
            File.WriteAllText(tempFile, "");
            expectedFilePaths.Add(tempFile);
        }

        var component = new DirectoryFilePathIteratorComponent {Path = tempDir};
        var harness = new ComponentHarness(component);

        var outputs = harness.Execute();

        Directory.Delete(tempDir, true);

        var filePaths = outputs.GetValueOrDefault(component.Out, Array.Empty<IArtifact>())
            .OfType<IFilePathArtifact>()
            .Select(artifact => artifact.FilePath)
            .ToHashSet();

        Assert.That(filePaths.SetEquals(expectedFilePaths));
    }
}