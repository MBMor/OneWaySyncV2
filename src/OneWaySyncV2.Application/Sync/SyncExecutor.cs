using OneWaySyncV2.Application.Abstractions;
using OneWaySyncV2.Domain.Sync;

namespace OneWaySyncV2.Application.Sync;

public sealed class SyncExecutor(
    IFileSystem fileSystem,
    ISyncLogger logger) : ISyncExecutor
{
    public async Task ExecuteAsync(
        SyncPlan plan,
        CancellationToken cancellationToken)
    {
        foreach (var operation in plan.Operations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ExecuteOperationAsync(operation, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await logger.ErrorAsync(
                    $"Failed to execute {operation.Type} for '{operation.RelativePath}'.",
                    ex,
                    cancellationToken);
            }
        }
    }

    private async Task ExecuteOperationAsync(
        SyncOperation operation,
        CancellationToken cancellationToken)
    {
        switch (operation.Type)
        {
            case SyncOperationType.Create:
                await CopyAsync(operation, overwrite: false, cancellationToken);
                break;

            case SyncOperationType.Update:
                await CopyAsync(operation, overwrite: true, cancellationToken);
                break;

            case SyncOperationType.Delete:
                await DeleteAsync(operation, cancellationToken);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported sync operation type: {operation.Type}");
        }
    }

    private async Task CopyAsync(
        SyncOperation operation,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        if (operation.SourcePath is null)
            throw new InvalidOperationException("Source path is required for copy operation.");

        if (operation.ReplicaPath is null)
            throw new InvalidOperationException("Replica path is required for copy operation.");

        await fileSystem.CopyFileAsync(
            operation.SourcePath,
            operation.ReplicaPath,
            overwrite,
            cancellationToken);

        await logger.InfoAsync(
            $"{operation.Type}: {operation.RelativePath}",
            cancellationToken);
    }

    private async Task DeleteAsync(
        SyncOperation operation,
        CancellationToken cancellationToken)
    {
        if (operation.ReplicaPath is null)
            throw new InvalidOperationException("Replica path is required for delete operation.");

        fileSystem.DeleteFile(operation.ReplicaPath);

        await logger.InfoAsync(
            $"Delete: {operation.RelativePath}",
            cancellationToken);
    }
}