namespace OneWaySyncV2.Application.Sync;

public sealed record SyncOptions(
    string Source,
    string Replica,
    TimeSpan Interval);