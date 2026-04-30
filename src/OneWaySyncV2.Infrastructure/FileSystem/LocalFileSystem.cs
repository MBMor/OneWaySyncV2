using OneWaySyncV2.Application.Abstractions;

namespace OneWaySyncV2.Infrastructure.FileSystem;

public sealed class LocalFileSystem(ISyncLogger logger) : IFileSystem
{
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public async Task<IReadOnlyCollection<DirectoryItem>> GetDirectoriesAsync(
        string rootPath,
        CancellationToken cancellationToken)
    {
        var directories = new List<DirectoryItem>();

        if (!Directory.Exists(rootPath))
            return directories;

        var directoriesToProcess = new Stack<string>();
        directoriesToProcess.Push(rootPath);

        while (directoriesToProcess.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentDirectory = directoriesToProcess.Pop();

            foreach (var directoryPath in await EnumerateDirectoriesSafeAsync(
                         currentDirectory,
                         cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    directories.Add(new DirectoryItem(
                        FullPath: directoryPath,
                        RelativePath: Path.GetRelativePath(rootPath, directoryPath)));

                    directoriesToProcess.Push(directoryPath);
                }
                catch (Exception ex) when (IsRecoverableFileSystemException(ex))
                {
                    await logger.ErrorAsync(
                        $"Failed to read directory metadata: {directoryPath}",
                        ex,
                        cancellationToken);
                }
            }
        }

        return directories;
    }

    public async Task<IReadOnlyCollection<FileItem>> GetFilesAsync(
        string rootPath,
        CancellationToken cancellationToken)
    {
        var files = new List<FileItem>();

        if (!Directory.Exists(rootPath))
            return files;

        var directoriesToProcess = new Stack<string>();
        directoriesToProcess.Push(rootPath);

        while (directoriesToProcess.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentDirectory = directoriesToProcess.Pop();

            foreach (var directory in await EnumerateDirectoriesSafeAsync(
                         currentDirectory,
                         cancellationToken))
            {
                directoriesToProcess.Push(directory);
            }

            foreach (var filePath in await EnumerateFilesSafeAsync(
                         currentDirectory,
                         cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var relativePath = Path.GetRelativePath(rootPath, filePath);
                    var metadata = GetFileMetadata(filePath);

                    files.Add(new FileItem(
                        FullPath: filePath,
                        RelativePath: relativePath,
                        Metadata: metadata));
                }
                catch (Exception ex) when (IsRecoverableFileSystemException(ex))
                {
                    await logger.ErrorAsync(
                        $"Failed to read file metadata: {filePath}",
                        ex,
                        cancellationToken);
                }
            }
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

        await using (var sourceStream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan))
        await using (var destinationStream = new FileStream(
            destinationPath,
            overwrite ? FileMode.Create : FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);
        }

        var lastWrite = File.GetLastWriteTimeUtc(sourcePath);
        File.SetLastWriteTimeUtc(destinationPath, lastWrite);
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

    private async Task<IReadOnlyCollection<string>> EnumerateDirectoriesSafeAsync(
    string directoryPath,
    CancellationToken cancellationToken)
    {
        try
        {
            return Directory
                .EnumerateDirectories(directoryPath)
                .ToList();
        }
        catch (Exception ex) when (IsRecoverableFileSystemException(ex))
        {
            await logger.ErrorAsync(
                $"Failed to enumerate directories in: {directoryPath}",
                ex,
                cancellationToken);

            return [];
        }
    }

    private async Task<IReadOnlyCollection<string>> EnumerateFilesSafeAsync(
        string directoryPath,
        CancellationToken cancellationToken)
    {
        try
        {
            return Directory
                .EnumerateFiles(directoryPath)
                .ToList();
        }
        catch (Exception ex) when (IsRecoverableFileSystemException(ex))
        {
            await logger.ErrorAsync(
                $"Failed to enumerate files in: {directoryPath}",
                ex,
                cancellationToken);

            return [];
        }
    }

    private static bool IsRecoverableFileSystemException(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException;
    }
}