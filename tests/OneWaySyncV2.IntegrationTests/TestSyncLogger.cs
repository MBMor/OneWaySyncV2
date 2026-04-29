using OneWaySyncV2.Application.Abstractions;

namespace OneWaySyncV2.IntegrationTests;

internal sealed class TestSyncLogger : ISyncLogger
{
    public Task InfoAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task WarningAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ErrorAsync(
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}