using FluentAssertions;
using OneWaySyncV2.Application.Sync;
using OneWaySyncV2.Domain.Sync;

namespace OneWaySyncV2.UnitTests.Sync;

public sealed class SyncExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCreateOperation_CopiesFile()
    {
        var fileSystem = new RecordingFileSystem();
        var logger = new TestSyncLogger();

        var executor = new SyncExecutor(fileSystem, logger);

        var plan = new SyncPlan([
            new SyncOperation(
                SyncOperationType.Create,
                "a.txt",
                "/source/a.txt",
                "/replica/a.txt")
        ]);

        await executor.ExecuteAsync(plan, CancellationToken.None);

        fileSystem.CopiedFiles.Should().ContainSingle(copy =>
            copy.SourcePath == "/source/a.txt"
            && copy.DestinationPath == "/replica/a.txt"
            && copy.Overwrite == false);

        logger.Infos.Should().Contain("Create: a.txt");
    }

    [Fact]
    public async Task ExecuteAsync_WhenUpdateOperation_CopiesFileWithOverwrite()
    {
        var fileSystem = new RecordingFileSystem();
        var logger = new TestSyncLogger();

        var executor = new SyncExecutor(fileSystem, logger);

        var plan = new SyncPlan([
            new SyncOperation(
                SyncOperationType.Update,
                "a.txt",
                "/source/a.txt",
                "/replica/a.txt")
        ]);

        await executor.ExecuteAsync(plan, CancellationToken.None);

        fileSystem.CopiedFiles.Should().ContainSingle(copy =>
            copy.Overwrite == true);

        logger.Infos.Should().Contain("Update: a.txt");
    }

    [Fact]
    public async Task ExecuteAsync_WhenDeleteOperation_DeletesReplicaFile()
    {
        var fileSystem = new RecordingFileSystem();
        var logger = new TestSyncLogger();

        var executor = new SyncExecutor(fileSystem, logger);

        var plan = new SyncPlan([
            new SyncOperation(
                SyncOperationType.Delete,
                "old.txt",
                SourcePath: null,
                ReplicaPath: "/replica/old.txt")
        ]);

        await executor.ExecuteAsync(plan, CancellationToken.None);

        fileSystem.DeletedFiles.Should().ContainSingle()
            .Which.Should().Be("/replica/old.txt");

        logger.Infos.Should().Contain("Delete: old.txt");
    }

    [Fact]
    public async Task ExecuteAsync_WhenFileOperationFails_LogsErrorAndContinues()
    {
        var fileSystem = new RecordingFileSystem
        {
            ThrowOnCopy = true
        };

        var logger = new TestSyncLogger();

        var executor = new SyncExecutor(fileSystem, logger);

        var plan = new SyncPlan([
            new SyncOperation(
                SyncOperationType.Create,
                "a.txt",
                "/source/a.txt",
                "/replica/a.txt"),

            new SyncOperation(
                SyncOperationType.Delete,
                "old.txt",
                SourcePath: null,
                ReplicaPath: "/replica/old.txt")
        ]);

        await executor.ExecuteAsync(plan, CancellationToken.None);

        logger.Errors.Should().ContainSingle();
        fileSystem.DeletedFiles.Should().Contain("/replica/old.txt");
    }
}