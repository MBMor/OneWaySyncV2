using OneWaySyncV2.Application.Abstractions;
using OneWaySyncV2.Application.Sync;
using OneWaySyncV2.Domain.Sync;

public sealed class SyncPlanner(
    IFileSystem fileSystem,
    IFileHasher fileHasher) : ISyncPlanner
{
    public async Task<SyncPlan> CreatePlanAsync(
        string sourcePath,
        string replicaPath,
        CancellationToken cancellationToken)
    {
        var sourceDirectories = await GetDirectoriesByRelativePathAsync(sourcePath, cancellationToken);
        var replicaDirectories = fileSystem.DirectoryExists(replicaPath)
            ? await GetDirectoriesByRelativePathAsync(replicaPath, cancellationToken)
            : [];

        var sourceFiles = await GetFilesByRelativePathAsync(sourcePath, cancellationToken);
        var replicaFiles = fileSystem.DirectoryExists(replicaPath)
            ? await GetFilesByRelativePathAsync(replicaPath, cancellationToken)
            : [];

        var operations = new List<SyncOperation>();

        AddCreateDirectoryOperations(
            operations,
            sourceDirectories,
            replicaDirectories,
            replicaPath,
            cancellationToken);

        await AddCreateAndUpdateFileOperationsAsync(
            operations,
            sourceFiles,
            replicaFiles,
            replicaPath,
            cancellationToken);

        AddDeleteFileOperations(
            operations,
            sourceFiles,
            replicaFiles,
            cancellationToken);

        AddDeleteDirectoryOperations(
            operations,
            sourceDirectories,
            replicaDirectories,
            cancellationToken);

        return new SyncPlan(operations);
    }

    private async Task<Dictionary<string, DirectoryItem>> GetDirectoriesByRelativePathAsync(
        string rootPath,
        CancellationToken cancellationToken)
    {
        var directories = await fileSystem.GetDirectoriesAsync(rootPath, cancellationToken);

        return directories.ToDictionary(
            directory => NormalizeRelativePath(directory.RelativePath),
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, FileItem>> GetFilesByRelativePathAsync(
        string rootPath,
        CancellationToken cancellationToken)
    {
        var files = await fileSystem.GetFilesAsync(rootPath, cancellationToken);

        return files.ToDictionary(
            file => NormalizeRelativePath(file.RelativePath),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AddCreateDirectoryOperations(
        ICollection<SyncOperation> operations,
        IReadOnlyDictionary<string, DirectoryItem> sourceDirectories,
        IReadOnlyDictionary<string, DirectoryItem> replicaDirectories,
        string replicaPath,
        CancellationToken cancellationToken)
    {
        foreach (var (relativePath, sourceDirectory) in sourceDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (replicaDirectories.ContainsKey(relativePath))
                continue;

            operations.Add(new SyncOperation(
                SyncOperationType.CreateDirectory,
                relativePath,
                SourcePath: sourceDirectory.FullPath,
                ReplicaPath: Path.Combine(replicaPath, relativePath)));
        }
    }

    private async Task AddCreateAndUpdateFileOperationsAsync(
        ICollection<SyncOperation> operations,
        IReadOnlyDictionary<string, FileItem> sourceFiles,
        IReadOnlyDictionary<string, FileItem> replicaFiles,
        string replicaPath,
        CancellationToken cancellationToken)
    {
        foreach (var (relativePath, sourceFile) in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!replicaFiles.TryGetValue(relativePath, out var replicaFile))
            {
                operations.Add(new SyncOperation(
                    SyncOperationType.Create,
                    relativePath,
                    SourcePath: sourceFile.FullPath,
                    ReplicaPath: Path.Combine(replicaPath, relativePath)));

                continue;
            }

            if (!await NeedsUpdateAsync(sourceFile, replicaFile, cancellationToken))
                continue;

            operations.Add(new SyncOperation(
                SyncOperationType.Update,
                relativePath,
                SourcePath: sourceFile.FullPath,
                ReplicaPath: replicaFile.FullPath));
        }
    }

    private static void AddDeleteFileOperations(
        ICollection<SyncOperation> operations,
        IReadOnlyDictionary<string, FileItem> sourceFiles,
        IReadOnlyDictionary<string, FileItem> replicaFiles,
        CancellationToken cancellationToken)
    {
        foreach (var (relativePath, replicaFile) in replicaFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (sourceFiles.ContainsKey(relativePath))
                continue;

            operations.Add(new SyncOperation(
                SyncOperationType.Delete,
                relativePath,
                SourcePath: null,
                ReplicaPath: replicaFile.FullPath));
        }
    }

    private static void AddDeleteDirectoryOperations(
        ICollection<SyncOperation> operations,
        IReadOnlyDictionary<string, DirectoryItem> sourceDirectories,
        IReadOnlyDictionary<string, DirectoryItem> replicaDirectories,
        CancellationToken cancellationToken)
    {
        foreach (var (relativePath, replicaDirectory) in replicaDirectories
                     .OrderByDescending(x => x.Key.Length))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (sourceDirectories.ContainsKey(relativePath))
                continue;

            operations.Add(new SyncOperation(
                SyncOperationType.DeleteDirectory,
                relativePath,
                SourcePath: null,
                ReplicaPath: replicaDirectory.FullPath));
        }
    }

    private async Task<bool> NeedsUpdateAsync(
        FileItem source,
        FileItem replica,
        CancellationToken cancellationToken)
    {
        if (source.Metadata.Length != replica.Metadata.Length)
            return true;

        if (source.Metadata.LastWriteTimeUtc == replica.Metadata.LastWriteTimeUtc)
            return false;

        var sourceHash = await fileHasher.ComputeHashAsync(
            source.FullPath,
            cancellationToken);

        var replicaHash = await fileHasher.ComputeHashAsync(
            replica.FullPath,
            cancellationToken);

        return sourceHash != replicaHash;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath.Replace(
            Path.AltDirectorySeparatorChar,
            Path.DirectorySeparatorChar);
    }
}