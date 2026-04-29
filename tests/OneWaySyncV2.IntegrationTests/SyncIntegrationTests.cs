using FluentAssertions;
using OneWaySyncV2.Application.Sync;
using OneWaySyncV2.Infrastructure.FileSystem;

namespace OneWaySyncV2.IntegrationTests.Sync;

public sealed class SyncIntegrationTests
{
    [Fact]
    public async Task Sync_WhenReplicaIsEmpty_CopiesAllSourceFiles()
    {
        using var source = new TemporaryDirectory();
        using var replica = new TemporaryDirectory();

        await File.WriteAllTextAsync(Path.Combine(source.Path, "a.txt"), "hello");
        Directory.CreateDirectory(Path.Combine(source.Path, "nested"));
        await File.WriteAllTextAsync(Path.Combine(source.Path, "nested", "b.txt"), "world");

        await RunSingleSyncAsync(source.Path, replica.Path);

        File.Exists(Path.Combine(replica.Path, "a.txt")).Should().BeTrue();
        File.Exists(Path.Combine(replica.Path, "nested", "b.txt")).Should().BeTrue();

        var content = await File.ReadAllTextAsync(Path.Combine(replica.Path, "a.txt"));
        content.Should().Be("hello");
    }

    [Fact]
    public async Task Sync_WhenSourceFileChanged_UpdatesReplicaFile()
    {
        using var source = new TemporaryDirectory();
        using var replica = new TemporaryDirectory();

        var sourceFile = Path.Combine(source.Path, "a.txt");
        var replicaFile = Path.Combine(replica.Path, "a.txt");

        await File.WriteAllTextAsync(sourceFile, "new");
        await File.WriteAllTextAsync(replicaFile, "old");

        File.SetLastWriteTimeUtc(sourceFile, DateTime.UtcNow);
        File.SetLastWriteTimeUtc(replicaFile, DateTime.UtcNow.AddMinutes(-10));

        await RunSingleSyncAsync(source.Path, replica.Path);

        var content = await File.ReadAllTextAsync(replicaFile);
        content.Should().Be("new");
    }

    [Fact]
    public async Task Sync_WhenReplicaHasExtraFile_DeletesIt()
    {
        using var source = new TemporaryDirectory();
        using var replica = new TemporaryDirectory();

        var extraFile = Path.Combine(replica.Path, "old.txt");
        await File.WriteAllTextAsync(extraFile, "remove me");

        await RunSingleSyncAsync(source.Path, replica.Path);

        File.Exists(extraFile).Should().BeFalse();
    }

    [Fact]
    public async Task Sync_WhenReplicaDoesNotExist_CreatesReplica()
    {
        using var source = new TemporaryDirectory();

        var replicaPath = Path.Combine(
            Path.GetTempPath(),
            $"one-way-sync-replica-{Guid.NewGuid():N}");

        try
        {
            await File.WriteAllTextAsync(Path.Combine(source.Path, "a.txt"), "hello");

            await RunSingleSyncAsync(source.Path, replicaPath);

            Directory.Exists(replicaPath).Should().BeTrue();
            File.Exists(Path.Combine(replicaPath, "a.txt")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(replicaPath))
                Directory.Delete(replicaPath, recursive: true);
        }
    }

    [Fact]
    public async Task Sync_WhenSourceIsEmpty_DeletesAllReplicaFiles()
    {
        using var source = new TemporaryDirectory();
        using var replica = new TemporaryDirectory();

        await File.WriteAllTextAsync(Path.Combine(replica.Path, "old.txt"), "old");

        Directory.CreateDirectory(Path.Combine(replica.Path, "nested"));
        await File.WriteAllTextAsync(Path.Combine(replica.Path, "nested", "old.txt"), "old");

        await RunSingleSyncAsync(source.Path, replica.Path);

        Directory.EnumerateFiles(replica.Path, "*", SearchOption.AllDirectories)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task Sync_WhenNestedFileChanged_UpdatesReplicaFile()
    {
        using var source = new TemporaryDirectory();
        using var replica = new TemporaryDirectory();

        var sourceDir = Path.Combine(source.Path, "nested");
        var replicaDir = Path.Combine(replica.Path, "nested");

        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(replicaDir);

        var sourceFile = Path.Combine(sourceDir, "a.txt");
        var replicaFile = Path.Combine(replicaDir, "a.txt");

        await File.WriteAllTextAsync(sourceFile, "new");
        await File.WriteAllTextAsync(replicaFile, "old");

        File.SetLastWriteTimeUtc(sourceFile, DateTime.UtcNow);
        File.SetLastWriteTimeUtc(replicaFile, DateTime.UtcNow.AddMinutes(-10));

        await RunSingleSyncAsync(source.Path, replica.Path);

        var content = await File.ReadAllTextAsync(replicaFile);
        content.Should().Be("new");
    }

    [Fact]
    public async Task Sync_WhenReplicaHasNestedExtraFile_DeletesIt()
    {
        using var source = new TemporaryDirectory();
        using var replica = new TemporaryDirectory();

        var replicaDir = Path.Combine(replica.Path, "nested");
        Directory.CreateDirectory(replicaDir);

        var extraFile = Path.Combine(replicaDir, "old.txt");
        await File.WriteAllTextAsync(extraFile, "remove me");

        await RunSingleSyncAsync(source.Path, replica.Path);

        File.Exists(extraFile).Should().BeFalse();
    }

    private static async Task RunSingleSyncAsync(
        string sourcePath,
        string replicaPath)
    {
        var fileSystem = new LocalFileSystem();
        var logger = new TestSyncLogger();

        fileSystem.CreateDirectory(replicaPath);

        var planner = new SyncPlanner(fileSystem);
        var executor = new SyncExecutor(fileSystem, logger);

        var plan = await planner.CreatePlanAsync(
            sourcePath,
            replicaPath,
            CancellationToken.None);

        await executor.ExecuteAsync(plan, CancellationToken.None);
    }
}