using OneWaySyncV2.Application.Abstractions;
using OneWaySyncV2.Domain.Sync;

namespace OneWaySyncV2.Application.Sync;

public sealed class SyncPlanner(IFileSystem fileSystem) : ISyncPlanner
{
    public async Task<SyncPlan> CreatePlanAsync(
        string sourcePath,
        string replicaPath,
        CancellationToken cancellationToken)
    {
        var sourceFiles = await fileSystem.GetFilesAsync(sourcePath, cancellationToken);
        var replicaFiles = fileSystem.DirectoryExists(replicaPath)
            ? await fileSystem.GetFilesAsync(replicaPath, cancellationToken)
            : [];

        var sourceByRelativePath = sourceFiles.ToDictionary(
            file => NormalizeRelativePath(file.RelativePath),
            StringComparer.OrdinalIgnoreCase);

        var replicaByRelativePath = replicaFiles.ToDictionary(
            file => NormalizeRelativePath(file.RelativePath),
            StringComparer.OrdinalIgnoreCase);

        var operations = new List<SyncOperation>();

        foreach (var sourceFile in sourceByRelativePath.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = NormalizeRelativePath(sourceFile.RelativePath);

            if (!replicaByRelativePath.TryGetValue(relativePath, out var replicaFile))
            {
                operations.Add(new SyncOperation(
                    SyncOperationType.Create,
                    relativePath,
                    sourceFile.FullPath,
                    Path.Combine(replicaPath, relativePath)));

                continue;
            }

            if (NeedsUpdate(sourceFile.Metadata, replicaFile.Metadata))
            {
                operations.Add(new SyncOperation(
                    SyncOperationType.Update,
                    relativePath,
                    sourceFile.FullPath,
                    replicaFile.FullPath));
            }
        }

        foreach (var replicaFile in replicaByRelativePath.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = NormalizeRelativePath(replicaFile.RelativePath);

            if (!sourceByRelativePath.ContainsKey(relativePath))
            {
                operations.Add(new SyncOperation(
                    SyncOperationType.Delete,
                    relativePath,
                    SourcePath: null,
                    replicaFile.FullPath));
            }
        }

        return new SyncPlan(operations);
    }

    private static bool NeedsUpdate(
        FileMetadata source,
        FileMetadata replica)
    {
        return source.Length != replica.Length
               || source.LastWriteTimeUtc != replica.LastWriteTimeUtc;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath.Replace(
            Path.AltDirectorySeparatorChar,
            Path.DirectorySeparatorChar);
    }
}