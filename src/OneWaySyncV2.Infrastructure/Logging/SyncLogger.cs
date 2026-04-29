using Microsoft.Extensions.Logging;
using OneWaySyncV2.Application.Abstractions;

namespace OneWaySyncV2.Infrastructure.Logging;

public sealed class SyncLogger(
    ILogger<SyncLogger> consoleLogger,
    ILogWriter fileLogWriter) : ISyncLogger
{
    public async Task InfoAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        consoleLogger.LogInformation("{Message}", message);

        await fileLogWriter.WriteAsync(
            OneWaySyncV2.Application.Abstractions.LogLevel.Information,
            message,
            cancellationToken: cancellationToken);
    }

    public async Task WarningAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        consoleLogger.LogWarning("{Message}", message);

        await fileLogWriter.WriteAsync(
            OneWaySyncV2.Application.Abstractions.LogLevel.Warning,
            message,
            cancellationToken: cancellationToken);
    }

    public async Task ErrorAsync(
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        consoleLogger.LogError(exception, "{Message}", message);

        await fileLogWriter.WriteAsync(
            OneWaySyncV2.Application.Abstractions.LogLevel.Error,
            message,
            exception,
            cancellationToken);
    }
}