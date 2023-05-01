using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ductwork.Artifacts;
using ductwork.Components;
using ductworkTests.TestHelpers;
using NUnit.Framework;

#nullable enable
namespace ductworkTests.Components;

public class CopyFilePathToTargetPathComponentTests
{
    [Test]
    public void ExecutesWithExpectedOutput()
    {
        var content = Guid.NewGuid().ToString();
        var root = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        var sourceRoot = Path.Join(root, "source");
        var targetRoot = Path.Join(root, "target");
        var sourceFilePath = Path.Join(root, Guid.NewGuid().ToString());

        Directory.CreateDirectory(sourceRoot);
        Directory.CreateDirectory(targetRoot);
        File.WriteAllText(sourceFilePath, content);

        var component = new CopyFilePathToTargetPathComponent {SourceRoot = sourceRoot, TargetRoot = targetRoot};
        var harness = new ComponentHarness(component);

        harness.QueuePush(component.In, new FilePathArtifact(sourceFilePath));

        var outputs = harness.Execute();

        var filePath = outputs.GetValueOrDefault(component.Out, Array.Empty<IArtifact>())
            .OfType<ITargetFilePathArtifact>()
            .Select(artifact => artifact.TargetFilePath)
            .FirstOrDefault();

        Assert.NotNull(filePath, $"Component did not push an `{nameof(ITargetFilePathArtifact)}` artifact " +
                                 $"to its `{nameof(component.Out)}` plug.");
        Assert.True(File.Exists(filePath), "Component claims it wrote to a file, but the file does not exist.");

        var fileContent = File.ReadAllText(filePath!);

        Directory.Delete(root, true);

        Assert.AreEqual(content, fileContent);
    }
}