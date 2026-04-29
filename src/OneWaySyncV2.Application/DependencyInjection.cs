using Microsoft.Extensions.DependencyInjection;
using OneWaySyncV2.Application.Sync;

namespace OneWaySyncV2.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ISyncPlanner, SyncPlanner>();
        services.AddSingleton<ISyncExecutor, SyncExecutor>();
        services.AddSingleton<ISyncRunner, SyncRunner>();

        return services;
    }
}