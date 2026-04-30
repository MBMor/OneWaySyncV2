namespace OneWaySyncV2.Cli;

internal static class CliUsage
{
    public static void Write(TextWriter writer)
    {
        writer.WriteLine("OneWaySyncV2");
        writer.WriteLine();
        writer.WriteLine("One-way directory synchronization tool.");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  OneWaySyncV2.Cli --source <path> --replica <path> --interval-seconds <seconds> --log-file <path>");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --source <path>              Source directory. Must exist.");
        writer.WriteLine("  --replica <path>             Replica directory. Created if it does not exist.");
        writer.WriteLine("  --interval-seconds <seconds> Sync interval in seconds. Must be greater than 0.");
        writer.WriteLine("  --log-file <path>            Path to log file. Parent directory is created if needed.");
        writer.WriteLine("  --help, -h                   Show help.");
        writer.WriteLine();
        writer.WriteLine("Rules:");
        writer.WriteLine("  - source and replica must be different directories.");
        writer.WriteLine("  - source cannot be inside replica.");
        writer.WriteLine("  - replica cannot be inside source.");
        writer.WriteLine();
        writer.WriteLine("Example:");
        writer.WriteLine("  OneWaySyncV2.Cli --source C:\\Data\\Source --replica C:\\Data\\Replica --interval-seconds 30 --log-file C:\\Logs\\sync.log");
    }
}