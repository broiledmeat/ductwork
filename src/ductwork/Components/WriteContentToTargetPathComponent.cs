using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ductwork.Artifacts;
using ductwork.Crates;
using ductwork.Executors;
using Force.Crc32;

namespace ductwork.Components;

public class WriteContentToTargetPathComponent : SingleInComponent
{
    protected override async Task ExecuteIn(IExecutor executor, ICrate crate, CancellationToken token)
    {
        if (crate.Get<ITargetPathArtifact>() is not { } targetFilePathArtifact ||
            crate.Get<IContentArtifact>() is not { } readSourceContentArtifact)
        {
            throw new InvalidOperationException($"Could not get either {nameof(ITargetPathArtifact)} " +
                                                $"or {nameof(IContentArtifact)} artifacts.");
        }

        var filePath = targetFilePathArtifact.TargetPath;
        var dirPath = Path.GetDirectoryName(filePath);

        if (dirPath == null)
        {
            throw new IOException($"Could not get directory name from {filePath}");
        }

        var content = await readSourceContentArtifact.GetContent(token);

        if (Path.Exists(filePath))
        {
            var targetContent = await File.ReadAllBytesAsync(filePath, token);
            var targetCrc = Crc32Algorithm.Compute(targetContent);
            var contentCrc = Crc32Algorithm.Compute(content);

            if (contentCrc == targetCrc)
            {
                executor.Log.Info($"{DisplayName}: {filePath}, skipped, target exists and contents are identical");
                return;
            }
        }

        Directory.CreateDirectory(dirPath);
        await File.WriteAllBytesAsync(filePath, content, token);
        executor.Log.Info($"{DisplayName}: {filePath}, written");
    }
}