using OneWaySyncV2.Application.Abstractions;

namespace OneWaySyncV2.Application.Sync;

public sealed class SyncRunner(
    IFileSystem fileSystem,
    ISyncPlanner planner,
    ISyncExecutor executor,
    ISyncLogger logger) : ISyncRunner
{
    public async Task RunAsync(
        SyncOptions options,
        CancellationToken cancellationToken)
    {
        fileSystem.CreateDirectory(options.Replica);

        using var timer = new PeriodicTimer(options.Interval);

        do
        {
            await RunCycleAsync(options, cancellationToken);
        }
        while (await timer.WaitForNextTickAsync(cancellationToken));
    }

    private async Task RunCycleAsync(
        SyncOptions options,
        CancellationToken cancellationToken)
    {
        await logger.InfoAsync("Sync cycle started.", cancellationToken);

        var plan = await planner.CreatePlanAsync(
            options.Source,
            options.Replica,
            cancellationToken);

        await logger.InfoAsync(
            $"Sync plan created. Operations: {plan.Operations.Count}",
            cancellationToken);

        await executor.ExecuteAsync(plan, cancellationToken);

        await logger.InfoAsync("Sync cycle completed.", cancellationToken);
    }
}