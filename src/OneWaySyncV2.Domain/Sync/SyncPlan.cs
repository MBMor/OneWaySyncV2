namespace OneWaySyncV2.Domain.Sync;

public sealed record SyncPlan(IReadOnlyCollection<SyncOperation> Operations)
{
    public bool HasChanges => Operations.Count > 0;
}