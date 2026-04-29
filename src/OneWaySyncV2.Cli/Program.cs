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
    var cliOptions = OptionsParser.Parse(args);

    using var cancellationTokenSource = new CancellationTokenSource();

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellationTokenSource.Cancel();
    };

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
            services.AddInfrastructure(cliOptions.LogFile);
        })
        .Build();

    var logger = host.Services.GetRequiredService<ISyncLogger>();
    var runner = host.Services.GetRequiredService<ISyncRunner>();

    await logger.InfoAsync("OneWaySync started. Press Ctrl+C to stop.");

    try
    {
        await runner.RunAsync(
            new SyncOptions(
                Source: cliOptions.Source,
                Replica: cliOptions.Replica,
                Interval: TimeSpan.FromSeconds(cliOptions.IntervalSeconds)),
            cancellationTokenSource.Token);
    }
    catch (OperationCanceledException)
    {
        await logger.InfoAsync("Shutdown requested.");
    }

    await logger.InfoAsync("OneWaySync stopped.");
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