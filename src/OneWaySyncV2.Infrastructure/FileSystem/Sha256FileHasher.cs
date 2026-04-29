using System.Security.Cryptography;
using OneWaySyncV2.Application.Abstractions;

namespace OneWaySyncV2.Infrastructure.FileSystem;

public sealed class Sha256FileHasher : IFileHasher
{
    public async Task<string> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        var hash = await SHA256.HashDataAsync(stream, cancellationToken);

        return Convert.ToHexString(hash);
    }
}