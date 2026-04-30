using OneWaySyncV2.Application.Abstractions;

namespace OneWaySyncV2.Infrastructure.Logging;

public sealed class FileLogWriter(string logFilePath) : ILogWriter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task WriteAsync(
        LogLevel level,
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(logFilePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var line = FormatLine(level, message, exception);

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            await File.AppendAllTextAsync(
                logFilePath,
                line + Environment.NewLine,
                cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static string FormatLine(
        LogLevel level,
        string message,
        Exception? exception)
    {
        var timestamp = DateTimeOffset.Now.ToString("O");

        if (exception is null)
            return $"[{timestamp}] [{level}] {message}";

        return $"[{timestamp}] [{level}] {message} Reason: {GetUserFriendlyError(exception)}";
    }

    private static string GetUserFriendlyError(Exception exception)
    {
        return exception switch
        {
            IOException => exception.Message,
            UnauthorizedAccessException => exception.Message,
            _ => $"{exception.GetType().Name}: {exception.Message}"
        };
    }
}