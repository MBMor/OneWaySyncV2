using OneWaySyncV2.Application.Abstractions;
using OneWaySyncV2.Application.Sync;
using OneWaySyncV2.Domain.Sync;

public sealed class SyncPlanner(IFileSystem fileSystem) : ISyncPlanner
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

        AddCreateAndUpdateFileOperations(
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

    private static void AddCreateAndUpdateFileOperations(
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

            if (!NeedsUpdate(sourceFile.Metadata, replicaFile.Metadata))
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