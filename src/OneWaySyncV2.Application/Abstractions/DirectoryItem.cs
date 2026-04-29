namespace OneWaySyncV2.Application.Abstractions;

public sealed record DirectoryItem(
    string FullPath,
    string RelativePath);