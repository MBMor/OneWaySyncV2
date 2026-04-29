using OneWaySyncV2.Application.Abstractions;

namespace OneWaySyncV2.UnitTests.Sync;

internal sealed class RecordingFileSystem : IFileSystem
{
    public bool ThrowOnCopy { get; init; }

    public List<CopyCall> CopiedFiles { get; } = [];

    public List<string> DeletedFiles { get; } = [];

    public bool DirectoryExists(string path) => true;

    public void CreateDirectory(string path)
    {
    }

    public Task<IReadOnlyCollection<FileItem>> GetFilesAsync(
        string rootPath,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<FileItem>>([]);
    }

    public Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        if (ThrowOnCopy)
            throw new IOException("Copy failed.");

        CopiedFiles.Add(new CopyCall(sourcePath, destinationPath, overwrite));

        return Task.CompletedTask;
    }

    public void DeleteFile(string path)
    {
        DeletedFiles.Add(path);
    }

    public void DeleteDirectory(string path)
    {
    }

    public FileMetadata GetFileMetadata(string path)
    {
        throw new NotSupportedException();
    }
}

internal sealed record CopyCall(
    string SourcePath,
    string DestinationPath,
    bool Overwrite);