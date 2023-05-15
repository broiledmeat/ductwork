using System;
using System.IO;
using ductwork.Artifacts;
using ductwork.Components;
using ductwork.Crates;
using ductworkTests.TestHelpers;
using NUnit.Framework;

#nullable enable
namespace ductworkTests.Components;

public class WriteContentToTargetPathComponentTests
{
    [Test]
    public void WritesContentFromTargetFilePath()
    {
        var targetRoot = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        var targetPath = Path.Join(targetRoot, Guid.NewGuid().ToString());
        var sourceContent = Guid.NewGuid().ToByteArray();

        var component = new WriteContentToTargetPathComponent();
        var harness = new ComponentHarness(component);

        var crate = harness.CreateCrate(
            new ContentArtifact(sourceContent),
            new TargetPathArtifact(targetPath));

        harness.QueuePush(component.In, crate);
        harness.Execute();

        Assert.That(File.Exists(targetPath));

        var targetContent = File.ReadAllBytes(targetPath);

        Assert.AreEqual(sourceContent, targetContent);
    }
}