using System;
using System.IO;
using System.Threading;
using ductwork.Artifacts;
using NUnit.Framework;

#nullable enable
namespace ductworkTests.Artifacts;

public class SourceFileArtifactTests
{
    [Test]
    public void GetContentEqualsSourceContent()
    {
        var root = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        var sourceFilePath = Path.Join(root, Guid.NewGuid().ToString());

        Directory.CreateDirectory(root);

        var sourceContent = Guid.NewGuid().ToByteArray();
        File.WriteAllBytes(sourceFilePath, sourceContent);

        var artifact = new SourcePathArtifact(sourceFilePath);
        var artifactContent = artifact.GetContent(CancellationToken.None).Result;

        Directory.Delete(root, true);

        Assert.AreEqual(sourceContent, artifactContent);
    }
}