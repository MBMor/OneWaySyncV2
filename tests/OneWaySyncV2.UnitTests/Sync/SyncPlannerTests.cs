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
                SourceFile("a.txt", 10)
            ],
            replicaFiles: []);

        var planner = new SyncPlanner(
            fileSystem,
            new InMemoryFileHasher(new Dictionary<string, string>()));

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
                SourceFile("a.txt", 20)
            ],
            replicaFiles:
            [
                ReplicaFile("a.txt", 10)
            ]);

        var planner = new SyncPlanner(
            fileSystem,
            new InMemoryFileHasher(new Dictionary<string, string>()));

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
                SourceFile("old.txt", 10)
            ]);

        var planner = new SyncPlanner(
            fileSystem,
            new InMemoryFileHasher(new Dictionary<string, string>()));

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
                SourceFile("a.txt", 10, lastWrite)
            ],
            replicaFiles:
            [
                ReplicaFile("a.txt", 10, lastWrite)
            ]);

        var planner = new SyncPlanner(
            fileSystem,
            new InMemoryFileHasher(new Dictionary<string, string>()));

        var plan = await planner.CreatePlanAsync(
            "/source",
            "/replica",
            CancellationToken.None);

        plan.Operations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePlanAsync_WhenTimestampDiffersButHashIsSame_ReturnsNoOperations()
    {
        var sourceTime = DateTimeOffset.UtcNow;
        var replicaTime = sourceTime.AddMinutes(-5);

        var sourceFile = SourceFile("a.txt", 10, sourceTime);
        var replicaFile = ReplicaFile("a.txt", 10, replicaTime);

        var fileSystem = new InMemoryFileSystem(
            sourceFiles: [sourceFile],
            replicaFiles: [replicaFile]);

        var fileHasher = new InMemoryFileHasher(new Dictionary<string, string>
        {
            [sourceFile.FullPath] = "ABC",
            [replicaFile.FullPath] = "ABC"
        });

        var planner = new SyncPlanner(fileSystem, fileHasher);

        var plan = await planner.CreatePlanAsync(
            "/source",
            "/replica",
            CancellationToken.None);

        plan.Operations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePlanAsync_WhenTimestampDiffersAndHashDiffers_ReturnsUpdateOperation()
    {
        var sourceTime = DateTimeOffset.UtcNow;
        var replicaTime = sourceTime.AddMinutes(-5);

        var sourceFile = SourceFile("a.txt", 10, sourceTime);
        var replicaFile = ReplicaFile("a.txt", 10, replicaTime);

        var fileSystem = new InMemoryFileSystem(
            sourceFiles: [sourceFile],
            replicaFiles: [replicaFile]);

        var fileHasher = new InMemoryFileHasher(new Dictionary<string, string>
        {
            [sourceFile.FullPath] = "ABC",
            [replicaFile.FullPath] = "DEF"
        });

        var planner = new SyncPlanner(fileSystem, fileHasher);

        var plan = await planner.CreatePlanAsync(
            "/source",
            "/replica",
            CancellationToken.None);

        var operation = plan.Operations.Should().ContainSingle().Subject;

        operation.Type.Should().Be(SyncOperationType.Update);
        operation.RelativePath.Should().Be("a.txt");
    }

    [Fact]
    public async Task CreatePlanAsync_WhenNestedFileIsMissing_ReturnsCreateDirectoryAndCreateFileOperations()
    {
        var fileSystem = new InMemoryFileSystem(
            sourceFiles:
            [
                SourceFile(Path.Combine("folder", "a.txt"), 10, DateTimeOffset.UtcNow)
            ],
            replicaFiles: []);

        var planner = new SyncPlanner(
            fileSystem,
            new InMemoryFileHasher(new Dictionary<string, string>()));

        var plan = await planner.CreatePlanAsync(
            "/source",
            "/replica",
            CancellationToken.None);

        plan.Operations.Should().HaveCount(2);

        plan.Operations.Should().Contain(operation =>
            operation.Type == SyncOperationType.CreateDirectory
            && operation.RelativePath == "folder");

        plan.Operations.Should().Contain(operation =>
            operation.Type == SyncOperationType.Create
            && operation.RelativePath == Path.Combine("folder", "a.txt"));
    }

    private static FileItem SourceFile(string relativePath, long length)
    {
        return SourceFile(relativePath, length, DateTimeOffset.UtcNow);
    }

    private static FileItem SourceFile(
        string relativePath,
        long length,
        DateTimeOffset lastWriteTimeUtc)
    {
        return new FileItem(
            FullPath: Path.Combine("/source", relativePath),
            RelativePath: relativePath,
            Metadata: new FileMetadata(
                Length: length,
                LastWriteTimeUtc: lastWriteTimeUtc));
    }

    private static FileItem ReplicaFile(string relativePath, long length)
    {
        return ReplicaFile(relativePath, length, DateTimeOffset.UtcNow);
    }

    private static FileItem ReplicaFile(
        string relativePath,
        long length,
        DateTimeOffset lastWriteTimeUtc)
    {
        return new FileItem(
            FullPath: Path.Combine("/replica", relativePath),
            RelativePath: relativePath,
            Metadata: new FileMetadata(
                Length: length,
                LastWriteTimeUtc: lastWriteTimeUtc));
    }
}