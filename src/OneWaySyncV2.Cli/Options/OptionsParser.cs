namespace OneWaySyncV2.Cli.Options;

public static class OptionsParser
{
    public static SyncOptions Parse(string[] args)
    {
        var values = ParseKeyValueArgs(args);

        var source = GetRequired(values, "--source");
        var replica = GetRequired(values, "--replica");
        var logFile = GetRequired(values, "--log-file");
        var intervalSecondsRaw = GetRequired(values, "--interval-seconds");

        if (!int.TryParse(intervalSecondsRaw, out var intervalSeconds) || intervalSeconds <= 0)
            throw new ArgumentException("--interval-seconds must be a positive integer.");

        var fullSource = Path.GetFullPath(source);
        var fullReplica = Path.GetFullPath(replica);
        var fullLogFile = Path.GetFullPath(logFile);

        ValidatePaths(fullSource, fullReplica, fullLogFile);

        return new SyncOptions(
            Source: fullSource,
            Replica: fullReplica,
            IntervalSeconds: intervalSeconds,
            LogFile: fullLogFile);
    }

    private static Dictionary<string, string> ParseKeyValueArgs(string[] args)
    {
        if (args.Length % 2 != 0)
            throw new ArgumentException("Arguments must be passed as key-value pairs.");

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i += 2)
        {
            var key = args[i];
            var value = args[i + 1];

            if (!key.StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Invalid argument '{key}'.");

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Value for '{key}' cannot be empty.");

            values[key] = value;
        }

        return values;
    }

    private static string GetRequired(
        IReadOnlyDictionary<string, string> values,
        string key)
    {
        if (!values.TryGetValue(key, out var value))
            throw new ArgumentException($"Missing required argument '{key}'.");

        return value;
    }

    private static void ValidatePaths(
        string source,
        string replica,
        string logFile)
    {
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException($"Source directory does not exist: {source}");

        if (PathsAreSame(source, replica))
            throw new ArgumentException("Source and replica must be different directories.");

        if (IsSubPath(source, replica))
            throw new ArgumentException("Replica cannot be inside source.");

        if (IsSubPath(replica, source))
            throw new ArgumentException("Source cannot be inside replica.");

        var logDirectory = Path.GetDirectoryName(logFile);

        if (string.IsNullOrWhiteSpace(logDirectory))
            throw new ArgumentException("Log file must contain a valid directory path.");
    }

    private static bool PathsAreSame(string first, string second)
    {
        return string.Equals(
            NormalizeDirectoryPath(first),
            NormalizeDirectoryPath(second),
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }

    private static bool IsSubPath(string parent, string child)
    {
        var normalizedParent = NormalizeDirectoryPath(parent);
        var normalizedChild = NormalizeDirectoryPath(child);

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return normalizedChild.StartsWith(normalizedParent, comparison)
               && !PathsAreSame(normalizedParent, normalizedChild);
    }

    private static string NormalizeDirectoryPath(string path)
    {
        var fullPath = Path.GetFullPath(path);

        return fullPath.EndsWith(Path.DirectorySeparatorChar)
            ? fullPath
            : fullPath + Path.DirectorySeparatorChar;
    }
}