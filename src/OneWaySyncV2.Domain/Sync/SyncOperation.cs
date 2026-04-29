namespace OneWaySyncV2.Domain.Sync;

public sealed record SyncOperation(
    SyncOperationType Type,
    string RelativePath,
    string? SourcePath,
    string? ReplicaPath);