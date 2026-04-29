namespace OneWaySyncV2.Application.Abstractions;

public sealed record FileMetadata(
    long Length,
    DateTimeOffset LastWriteTimeUtc);