namespace OneWaySyncV2.Application.Abstractions;

public interface IFileHasher
{
    Task<string> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken);
}