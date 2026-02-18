namespace EasySave.LogServer.Tests;

public class ServerPathProviderTests : IDisposable
{
    private readonly string _tempDir;

    public ServerPathProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"logserver_tests_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Constructor_CreatesDirectory()
    {
        _ = new ServerPathProvider(_tempDir);

        Assert.True(Directory.Exists(_tempDir));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsOnNullOrWhitespace(string? path)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ServerPathProvider(path!));
    }

    [Fact]
    public void GetDailyLogPath_Json_ReturnsJsonExtension()
    {
        var provider = new ServerPathProvider(_tempDir);
        var date = new DateTime(2025, 3, 15);

        var path = provider.GetDailyLogPath(date, LogFormat.Json);

        Assert.Equal(Path.Combine(_tempDir, "2025-03-15.json"), path);
    }

    [Fact]
    public void GetDailyLogPath_Xml_ReturnsXmlExtension()
    {
        var provider = new ServerPathProvider(_tempDir);
        var date = new DateTime(2025, 3, 15);

        var path = provider.GetDailyLogPath(date, LogFormat.Xml);

        Assert.Equal(Path.Combine(_tempDir, "2025-03-15.xml"), path);
    }

    [Fact]
    public void GetDailyLogPath_DefaultFormat_ReturnsJson()
    {
        var provider = new ServerPathProvider(_tempDir);
        var date = new DateTime(2025, 1, 1);

        var path = provider.GetDailyLogPath(date);

        Assert.EndsWith(".json", path);
    }

    [Fact]
    public void GetStatePath_ThrowsNotSupported()
    {
        var provider = new ServerPathProvider(_tempDir);

        Assert.Throws<NotSupportedException>(() => provider.GetStatePath());
    }

    [Fact]
    public void GetJobsConfigPath_ThrowsNotSupported()
    {
        var provider = new ServerPathProvider(_tempDir);

        Assert.Throws<NotSupportedException>(() => provider.GetJobsConfigPath());
    }

    [Fact]
    public void GetUserPreferencesPath_ThrowsNotSupported()
    {
        var provider = new ServerPathProvider(_tempDir);

        Assert.Throws<NotSupportedException>(() => provider.GetUserPreferencesPath());
    }

    [Fact]
    public void ResolveLogsDirectory_ReturnsConfiguredDirectory()
    {
        var provider = new ServerPathProvider(_tempDir);

        Assert.Equal(_tempDir, provider.ResolveLogsDirectory());
    }

    [Fact]
    public void SetLogDirectoryOverride_DoesNotThrow()
    {
        var provider = new ServerPathProvider(_tempDir);

        var ex = Record.Exception(() => provider.SetLogDirectoryOverride("/some/other/path"));

        Assert.Null(ex);
    }
}
