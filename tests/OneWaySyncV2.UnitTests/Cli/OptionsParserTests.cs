using OneWaySyncV2.Application.Abstractions;
using OneWaySyncV2.Cli.Options;
using OneWaySyncV2.UnitTests.Helpers;

namespace OneWaySyncV2.UnitTests.Cli;

public sealed class OptionsParserTests
{
    [Fact]
    public void Parse_WhenSourceDoesNotExist_Throws()
    {
        var args = new[]
        {
            "--source", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            "--replica", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            "--interval-seconds", "5",
            "--log-file", Path.Combine(Path.GetTempPath(), "sync.log")
        };

        Assert.Throws<DirectoryNotFoundException>(() => OptionsParser.Parse(args));
    }

    [Fact]
    public void Parse_WhenIntervalIsInvalid_Throws()
    {
        using var temp = new TemporaryDirectory();

        var args = new[]
        {
            "--source", temp.Path,
            "--replica", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            "--interval-seconds", "0",
            "--log-file", Path.Combine(Path.GetTempPath(), "sync.log")
        };

        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(args));
    }

    [Fact]
    public void Parse_WhenReplicaIsInsideSource_Throws()
    {
        using var temp = new TemporaryDirectory();

        var replica = Path.Combine(temp.Path, "replica");

        var args = new[]
        {
            "--source", temp.Path,
            "--replica", replica,
            "--interval-seconds", "5",
            "--log-file", Path.Combine(Path.GetTempPath(), "sync.log")
        };

        Assert.Throws<ArgumentException>(() => OptionsParser.Parse(args));
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