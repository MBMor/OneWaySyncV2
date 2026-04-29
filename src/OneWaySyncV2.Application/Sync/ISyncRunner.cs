namespace OneWaySyncV2.Application.Sync;

public interface ISyncRunner
{
    Task RunAsync(
        SyncOptions options,
        CancellationToken cancellationToken);
}