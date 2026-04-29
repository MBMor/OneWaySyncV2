using OneWaySyncV2.Application.Abstractions;

namespace OneWaySyncV2.UnitTests.Helpers;

internal sealed class InMemoryFileSystem(
    IReadOnlyCollection<FileItem> sourceFiles,
    IReadOnlyCollection<FileItem> replicaFiles) : IFileSystem
{
    public bool DirectoryExists(string path) => true;

    public void CreateDirectory(string path)
    {
    }

    public Task<IReadOnlyCollection<FileItem>> GetFilesAsync(
        string rootPath,
        CancellationToken cancellationToken)
    {
        var files = rootPath.Contains("source", StringComparison.OrdinalIgnoreCase)
            ? sourceFiles
            : replicaFiles;

        return Task.FromResult(files);
    }

    public Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void DeleteFile(string path)
    {
    }

    public void DeleteDirectory(string path)
    {
    }

    public FileMetadata GetFileMetadata(string path)
    {
        throw new NotSupportedException();
    }
}