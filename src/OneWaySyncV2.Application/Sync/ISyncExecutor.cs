using OneWaySyncV2.Domain.Sync;

namespace OneWaySyncV2.Application.Sync;

public interface ISyncExecutor
{
    Task ExecuteAsync(
        SyncPlan plan,
        CancellationToken cancellationToken);
}