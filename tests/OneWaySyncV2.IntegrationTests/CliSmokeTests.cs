using System.Diagnostics;
using FluentAssertions;

namespace OneWaySyncV2.IntegrationTests;

public sealed class CliSmokeTests
{
    [Fact]
    public async Task Cli_WhenValidArgumentsAreProvided_SynchronizesFilesAndWritesLog()
    {
        using var source = new TemporaryDirectory();
        using var replica = new TemporaryDirectory();
        using var logs = new TemporaryDirectory();

        var sourceFile = Path.Combine(source.Path, "a.txt");
        var replicaFile = Path.Combine(replica.Path, "a.txt");
        var logFile = Path.Combine(logs.Path, "sync.log");

        await File.WriteAllTextAsync(sourceFile, "hello");

        using var process = StartCliProcess(
            source.Path,
            replica.Path,
            logFile);

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        try
        {
            await WaitUntilAsync(
                process,
                condition: () => File.Exists(replicaFile) && File.Exists(logFile),
                timeout: TimeSpan.FromSeconds(30));

            File.Exists(replicaFile).Should().BeTrue();

            var replicaContent = await File.ReadAllTextAsync(replicaFile);
            replicaContent.Should().Be("hello");

            File.Exists(logFile).Should().BeTrue();

            var logContent = await File.ReadAllTextAsync(logFile);
            logContent.Should().Contain("OneWaySync started");
            logContent.Should().Contain("Create: a.txt");
        }
        finally
        {
            StopProcess(process);
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        stderr.Should().BeEmpty($"CLI stderr should be empty. Stdout: {stdout}");
    }

    private static Process StartCliProcess(
        string sourcePath,
        string replicaPath,
        string logFilePath)
    {
        var projectPath = GetCliProjectPath();

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--source");
        startInfo.ArgumentList.Add(sourcePath);
        startInfo.ArgumentList.Add("--replica");
        startInfo.ArgumentList.Add(replicaPath);
        startInfo.ArgumentList.Add("--interval-seconds");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add("--log-file");
        startInfo.ArgumentList.Add(logFilePath);

        return Process.Start(startInfo)
               ?? throw new InvalidOperationException("Failed to start CLI process.");
    }

    private static async Task WaitUntilAsync(
        Process process,
        Func<bool> condition,
        TimeSpan timeout)
    {
        var startedAt = DateTimeOffset.UtcNow;

        while (!condition())
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"CLI process exited before condition was met. Exit code: {process.ExitCode}");
            }

            if (DateTimeOffset.UtcNow - startedAt > timeout)
            {
                throw new TimeoutException(
                    $"Condition was not met within {timeout.TotalSeconds} seconds.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }
    }

    private static void StopProcess(Process process)
    {
        if (process.HasExited)
            return;

        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(milliseconds: 5_000);
        }
        catch
        {
            // Test cleanup only.
        }
    }

    private static string GetCliProjectPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        var directory = new DirectoryInfo(currentDirectory);

        while (directory is not null)
        {
            var projectPath = Path.Combine(
                directory.FullName,
                "src",
                "OneWaySyncV2.Cli",
                "OneWaySyncV2.Cli.csproj");

            if (File.Exists(projectPath))
                return projectPath;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find OneWaySyncV2.Cli.csproj.");
    }
}