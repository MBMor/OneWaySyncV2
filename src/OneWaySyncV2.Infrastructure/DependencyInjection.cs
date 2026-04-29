using Microsoft.Extensions.DependencyInjection;
using OneWaySyncV2.Application.Abstractions;
using OneWaySyncV2.Infrastructure.Logging;

namespace OneWaySyncV2.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string logFilePath)
    {
        services.AddSingleton<ILogWriter>(_ => new FileLogWriter(logFilePath));
        services.AddSingleton<ISyncLogger, SyncLogger>();

        return services;
    }
}