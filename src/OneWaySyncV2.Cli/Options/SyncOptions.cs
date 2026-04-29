namespace OneWaySyncV2.Cli.Options;

public sealed record SyncOptions(
    string Source,
    string Replica,
    int IntervalSeconds,
    string LogFile
    );
