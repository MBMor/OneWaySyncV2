using OneWaySyncV2.Application.Abstractions;

namespace OneWaySyncV2.UnitTests.Sync;

internal sealed class TestSyncLogger : ISyncLogger
{
    public List<string> Infos { get; } = [];

    public List<string> Warnings { get; } = [];

    public List<string> Errors { get; } = [];

    public Task InfoAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        Infos.Add(message);
        return Task.CompletedTask;
    }

    public Task WarningAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        Warnings.Add(message);
        return Task.CompletedTask;
    }

    public Task ErrorAsync(
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        Errors.Add(message);
        return Task.CompletedTask;
    }
}