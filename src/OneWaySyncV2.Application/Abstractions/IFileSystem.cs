namespace OneWaySyncV2.Application.Abstractions;

public interface IFileSystem
{
    bool DirectoryExists(string path);

    void CreateDirectory(string path);

    Task<IReadOnlyCollection<FileItem>> GetFilesAsync(
        string rootPath,
        CancellationToken cancellationToken);

    Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite,
        CancellationToken cancellationToken);

    void DeleteFile(string path);

    void DeleteDirectory(string path);

    FileMetadata GetFileMetadata(string path);
}