using FluentAssertions;
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

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("--HELP")]
    public void IsHelpRequested_WhenHelpArgumentIsPresent_ReturnsTrue(string helpArgument)
    {
        var result = OptionsParser.IsHelpRequested([helpArgument]);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsHelpRequested_WhenHelpArgumentIsMissing_ReturnsFalse()
    {
        var result = OptionsParser.IsHelpRequested([
            "--source", "/source",
        "--replica", "/replica",
        "--interval-seconds", "10",
        "--log-file", "/logs/sync.log"
        ]);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsHelpRequested_WhenHelpIsMixedWithOtherArguments_ReturnsTrue()
    {
        var result = OptionsParser.IsHelpRequested([
            "--source", "/source",
        "--replica", "/replica",
        "--interval-seconds", "10",
        "--log-file", "/logs/sync.log",
        "--help"
        ]);

        result.Should().BeTrue();
    }
}