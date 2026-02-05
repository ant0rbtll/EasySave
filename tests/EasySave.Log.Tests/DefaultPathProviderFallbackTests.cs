using EasySave.Configuration;

namespace EasySave.Log.Tests;

public class DefaultPathProviderFallbackTests
{
    [Fact]
    public void GetDailyLogPath_FallsBackToDefaultWhenOverrideIsFilePath()
    {
        var provider = new DefaultPathProvider();
        var date = new DateTime(2026, 2, 5);

        var tempFile = Path.Combine(Path.GetTempPath(), $"EasySaveLogFallback_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "x");

        try
        {
            provider.SetLogDirectoryOverride(tempFile);

            var path = provider.GetDailyLogPath(date);
            var expectedDir = Path.Combine(AppContext.BaseDirectory, "logs");
            var expectedPath = Path.Combine(expectedDir, $"{date:yyyy-MM-dd}.json");

            Assert.Equal(expectedPath, path);
            Assert.True(File.Exists(expectedPath));
        }
        finally
        {
            provider.SetLogDirectoryOverride(null);

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
