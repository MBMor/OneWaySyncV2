namespace OneWaySyncV2.Cli.Options;

public sealed record CliOptions(
    string Source,
    string Replica,
    int IntervalSeconds,
    string LogFile
    );
