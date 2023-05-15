using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ductwork;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Crates;
using ductworkTests.TestHelpers;
using NUnit.Framework;

namespace ductworkTests.Components;

public class RootSourcePathIteratorComponentTests
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

        var component = new RootSourcePathIteratorComponent {SourceRoot = tempDir};
        var harness = new ComponentHarness(component);

        var outputs = harness.Execute();

        Directory.Delete(tempDir, true);

        var filePaths = outputs.GetValueOrDefault(component.Out, Array.Empty<ICrate>())
            .Select(crate => crate.Get<ISourcePathArtifact>())
            .NotNull()
            .Select(artifact => artifact.SourcePath)
            .ToHashSet();

        Assert.That(filePaths.SetEquals(expectedFilePaths));
    }
}