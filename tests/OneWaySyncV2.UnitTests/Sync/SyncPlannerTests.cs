using FluentAssertions;
using OneWaySyncV2.Application.Abstractions;
using OneWaySyncV2.Application.Sync;
using OneWaySyncV2.Domain.Sync;
using OneWaySyncV2.UnitTests.Helpers;

namespace OneWaySyncV2.UnitTests.Sync;

public sealed class SyncPlannerTests
{
    [Fact]
    public async Task CreatePlanAsync_WhenReplicaIsMissingFile_ReturnsCreateOperation()
    {
        var fileSystem = new InMemoryFileSystem(
            sourceFiles:
            [
                File("a.txt", 10)
            ],
            replicaFiles: []);

        var planner = new SyncPlanner(fileSystem);

        var plan = await planner.CreatePlanAsync(
            "/source",
            "/replica",
            CancellationToken.None);

        var operation = Assert.Single(plan.Operations);

        Assert.Equal(SyncOperationType.Create, operation.Type);
        Assert.Equal("a.txt", operation.RelativePath);
    }

    [Fact]
    public async Task CreatePlanAsync_WhenReplicaHasOldFile_ReturnsUpdateOperation()
    {
        var fileSystem = new InMemoryFileSystem(
            sourceFiles:
            [
                File("a.txt", 20)
            ],
            replicaFiles:
            [
                File("a.txt", 10)
            ]);

        var planner = new SyncPlanner(fileSystem);

        var plan = await planner.CreatePlanAsync(
            "/source",
            "/replica",
            CancellationToken.None);

        var operation = Assert.Single(plan.Operations);

        Assert.Equal(SyncOperationType.Update, operation.Type);
        Assert.Equal("a.txt", operation.RelativePath);
    }

    [Fact]
    public async Task CreatePlanAsync_WhenReplicaHasExtraFile_ReturnsDeleteOperation()
    {
        var fileSystem = new InMemoryFileSystem(
            sourceFiles: [],
            replicaFiles:
            [
                File("old.txt", 10)
            ]);

        var planner = new SyncPlanner(fileSystem);

        var plan = await planner.CreatePlanAsync(
            "/source",
            "/replica",
            CancellationToken.None);

        var operation = Assert.Single(plan.Operations);

        Assert.Equal(SyncOperationType.Delete, operation.Type);
        Assert.Equal("old.txt", operation.RelativePath);
    }

    [Fact]
    public async Task CreatePlanAsync_WhenFilesAreSame_ReturnsNoOperations()
    {
        var lastWrite = DateTimeOffset.UtcNow;

        var fileSystem = new InMemoryFileSystem(
            sourceFiles:
            [
                File("a.txt", 10, lastWrite)
            ],
            replicaFiles:
            [
                File("a.txt", 10, lastWrite)
            ]);

        var planner = new SyncPlanner(fileSystem);

        var plan = await planner.CreatePlanAsync(
            "/source",
            "/replica",
            CancellationToken.None);

        plan.Operations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePlanAsync_WhenTimestampDiffers_ReturnsUpdateOperation()
    {
        var fileSystem = new InMemoryFileSystem(
            sourceFiles:
            [
                File("a.txt", 10, DateTimeOffset.UtcNow)
            ],
            replicaFiles:
            [
                File("a.txt", 10, DateTimeOffset.UtcNow.AddMinutes(-5))
            ]);

        var planner = new SyncPlanner(fileSystem);

        var plan = await planner.CreatePlanAsync(
            "/source",
            "/replica",
            CancellationToken.None);

        var operation = plan.Operations.Should().ContainSingle().Subject;

        operation.Type.Should().Be(SyncOperationType.Update);
        operation.RelativePath.Should().Be("a.txt");
    }

    [Fact]
    public async Task CreatePlanAsync_WhenNestedFileIsMissing_ReturnsCreateOperation()
    {
        var fileSystem = new InMemoryFileSystem(
            sourceFiles:
            [
                File(Path.Combine("folder", "a.txt"), 10, DateTimeOffset.UtcNow)
            ],
            replicaFiles: []);

        var planner = new SyncPlanner(fileSystem);

        var plan = await planner.CreatePlanAsync(
            "/source",
            "/replica",
            CancellationToken.None);

        var operation = plan.Operations.Should().ContainSingle().Subject;

        operation.Type.Should().Be(SyncOperationType.Create);
        operation.RelativePath.Should().Be(Path.Combine("folder", "a.txt"));
    }

    private static FileItem File(string relativePath, long length)
    {
        return File(relativePath, length, DateTimeOffset.UtcNow);
    }

    private static FileItem File(
        string relativePath,
        long length,
        DateTimeOffset lastWriteTimeUtc)
    {
        return new FileItem(
            FullPath: relativePath,
            RelativePath: relativePath,
            Metadata: new FileMetadata(
                Length: length,
                LastWriteTimeUtc: lastWriteTimeUtc));
    }
}