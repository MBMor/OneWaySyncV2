using OneWaySyncV2.Domain.Sync;

namespace OneWaySyncV2.Application.Sync;

public interface ISyncPlanner
{
    Task<SyncPlan> CreatePlanAsync(
        string sourcePath,
        string replicaPath,
        CancellationToken cancellationToken);
}