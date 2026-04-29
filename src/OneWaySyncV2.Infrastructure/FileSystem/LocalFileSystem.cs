using OneWaySyncV2.Application.Abstractions;

namespace OneWaySyncV2.Infrastructure.FileSystem;

public sealed class LocalFileSystem : IFileSystem
{
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public async Task<IReadOnlyCollection<FileItem>> GetFilesAsync(
        string rootPath,
        CancellationToken cancellationToken)
    {
        var files = new List<FileItem>();

        foreach (var filePath in Directory.EnumerateFiles(
                     rootPath,
                     "*",
                     SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(rootPath, filePath);
            var metadata = GetFileMetadata(filePath);

            files.Add(new FileItem(
                FullPath: filePath,
                RelativePath: relativePath,
                Metadata: metadata));
        }

        return files;
    }

    public async Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath);

        if (!string.IsNullOrWhiteSpace(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        await using var sourceStream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using var destinationStream = new FileStream(
            destinationPath,
            overwrite ? FileMode.Create : FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await sourceStream.CopyToAsync(destinationStream, cancellationToken);
    }

    public void DeleteFile(string path)
    {
        File.Delete(path);
    }

    public void DeleteDirectory(string path)
    {
        Directory.Delete(path, recursive: true);
    }

    public FileMetadata GetFileMetadata(string path)
    {
        var info = new FileInfo(path);

        return new FileMetadata(
            Length: info.Length,
            LastWriteTimeUtc: info.LastWriteTimeUtc);
    }
}