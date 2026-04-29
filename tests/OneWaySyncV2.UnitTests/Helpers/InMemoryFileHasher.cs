using OneWaySyncV2.Application.Abstractions;

namespace OneWaySyncV2.UnitTests.Helpers;

internal sealed class InMemoryFileHasher(
    IReadOnlyDictionary<string, string> hashes) : IFileHasher
{
    public Task<string> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(hashes[filePath]);
    }
}