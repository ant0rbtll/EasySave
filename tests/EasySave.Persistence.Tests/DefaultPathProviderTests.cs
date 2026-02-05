using EasySave.Configuration;

namespace EasySave.Persistence.Tests;

public class DefaultPathProviderTests
{
    [Fact]
    public void GetDailyLogPath_WhenOverrideIsNull_UsesDefaultLogsDirectory()
    {
        var provider = new DefaultPathProvider();
        var date = new DateTime(2026, 2, 5);

        provider.SetLogDirectoryOverride("custom-logs");
        provider.SetLogDirectoryOverride(null);

        var path = provider.GetDailyLogPath(date);
        var expectedDir = Path.Combine(AppContext.BaseDirectory, "logs");
        var expectedPath = Path.Combine(expectedDir, $"{date:yyyy-MM-dd}.json");

        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDailyLogPath_WithRelativeOverride_ResolvesAgainstBaseDirectory()
    {
        var provider = new DefaultPathProvider();
        var date = new DateTime(2026, 2, 5);
        var relative = "custom-logs";
        var expectedDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relative));

        try
        {
            provider.SetLogDirectoryOverride(relative);
            var path = provider.GetDailyLogPath(date);

            Assert.Equal(Path.Combine(expectedDir, $"{date:yyyy-MM-dd}.json"), path);
        }
        finally
        {
            if (Directory.Exists(expectedDir))
            {
                Directory.Delete(expectedDir, true);
            }
        }
    }

    [Fact]
    public void GetDailyLogPath_WithAbsoluteOverride_UsesAbsolutePath()
    {
        var provider = new DefaultPathProvider();
        var date = new DateTime(2026, 2, 5);
        var absolute = Path.Combine(Path.GetTempPath(), $"EasySaveLogs_{Guid.NewGuid():N}");

        try
        {
            provider.SetLogDirectoryOverride(absolute);
            var path = provider.GetDailyLogPath(date);

            Assert.Equal(Path.Combine(absolute, $"{date:yyyy-MM-dd}.json"), path);
        }
        finally
        {
            if (Directory.Exists(absolute))
            {
                Directory.Delete(absolute, true);
            }
        }
    }

    [Fact]
    public void SetLogDirectoryOverride_TrimsWhitespace()
    {
        var provider = new DefaultPathProvider();
        var date = new DateTime(2026, 2, 5);
        var relative = "trim-logs";
        var expectedDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relative));

        try
        {
            provider.SetLogDirectoryOverride($"  {relative}  ");
            var path = provider.GetDailyLogPath(date);

            Assert.Equal(Path.Combine(expectedDir, $"{date:yyyy-MM-dd}.json"), path);
        }
        finally
        {
            if (Directory.Exists(expectedDir))
            {
                Directory.Delete(expectedDir, true);
            }
        }
    }
}
