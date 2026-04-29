namespace OneWaySyncV2.UnitTests.Helpers;

public sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

    public TemporaryDirectory()
    {
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}