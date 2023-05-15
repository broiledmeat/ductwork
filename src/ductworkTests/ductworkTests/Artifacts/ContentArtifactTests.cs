#nullable enable
using System;
using System.Threading;
using ductwork.Artifacts;
using NUnit.Framework;

namespace ductworkTests.Artifacts;

public class ContentArtifactTests
{
    [Test]
    public void GetContentEqualsSourceContent()
    {
        var sourceContent = Guid.NewGuid().ToByteArray();
        
        var artifact = new ContentArtifact(sourceContent);
        var artifactContent = artifact.GetContent(CancellationToken.None).Result;

        Assert.AreEqual(sourceContent, artifactContent);
    }
}