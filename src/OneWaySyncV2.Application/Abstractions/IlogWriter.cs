namespace OneWaySyncV2.Application.Abstractions;

public interface ILogWriter
{
    Task WriteAsync(
        LogLevel level,
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default);
}