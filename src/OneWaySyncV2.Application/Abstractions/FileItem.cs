namespace OneWaySyncV2.Application.Abstractions;

public sealed record FileItem(
    string FullPath,
    string RelativePath,
    FileMetadata Metadata);