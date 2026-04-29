using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneWaySyncV2.Application;
using OneWaySyncV2.Application.Abstractions;
using OneWaySyncV2.Application.Sync;
using OneWaySyncV2.Cli.Options;
using OneWaySyncV2.Infrastructure;

//--source "C:\_test\A"--replica "C:\_test\B" --interval-seconds 30 --log-file "C:\_test\sync.log"

try
{
    var options = OptionsParser.Parse(args);

    using var host = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            });
        })
        .ConfigureServices(services =>
        {
            services.AddApplication();
            services.AddInfrastructure(options.LogFile);
        })
        .Build();

    var logger = host.Services.GetRequiredService<ISyncLogger>();
    var planner = host.Services.GetRequiredService<ISyncPlanner>();
    var executor = host.Services.GetRequiredService<ISyncExecutor>();

    await logger.InfoAsync("OneWaySync started.");

    var plan = await planner.CreatePlanAsync(
        options.Source,
        options.Replica,
        CancellationToken.None);

    await logger.InfoAsync($"Sync plan created. Operations: {plan.Operations.Count}");

    await executor.ExecuteAsync(plan, CancellationToken.None);

    await logger.InfoAsync("Sync completed.");
}
catch (Exception ex) when (
    ex is ArgumentException or DirectoryNotFoundException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Invalid arguments: {ex.Message}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  OneWaySyncV2.Cli --source <path> --replica <path> --interval-seconds <seconds> --log-file <path>");

    return 1;
}

return 0;