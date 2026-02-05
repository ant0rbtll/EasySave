using System.Text.Json;
using EasySave.Configuration;
using EasySave.Log;

namespace EasyLog.Tests;

public class DailyFileLoggerUnitTests
{
    [Fact]
    public void Write_NormalizesPathsAndUtcTimestamp()
    {
        using var tempDir = new TempDirectory();
        var local = DateTime.SpecifyKind(new DateTime(2026, 2, 5, 8, 30, 0), DateTimeKind.Local);
        var expectedUtc = local.ToUniversalTime();
        var expectedDate = expectedUtc.Date;

        var pathProvider = new TestPathProvider(tempDir.Path);
        using var logger = new DailyFileLogger(new JsonLogFormatter(), pathProvider);

        var entry = new EasySave.Log.LogEntry(
            local,
            "JobPaths",
            LogEventType.TransferFile,
            "  /a/b  ",
            "  c:/d/e  ",
            1,
            2);

        logger.Write(entry);

        Assert.Equal(expectedDate, pathProvider.LastRequestedDate);

        var logPath = pathProvider.GetDailyLogPath(expectedDate);
        var logged = ReadLogEntries(logPath).Single();

        Assert.Equal(expectedUtc, logged.Timestamp);
        Assert.Equal(DateTimeKind.Utc, logged.Timestamp.Kind);
        Assert.Equal("\\a\\b", logged.SourcePathUNC);
        Assert.Equal("c:\\d\\e", logged.DestinationPathUNC);
    }

    [Fact]
    public void Write_ThrowsOnNullEntry()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        using var logger = new DailyFileLogger(new JsonLogFormatter(), pathProvider);

        Assert.Throws<ArgumentNullException>(() => logger.Write(null!));
    }

    [Fact]
    public void Write_AppendsToExistingArrayFile()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var pathProvider = new TestPathProvider(tempDir.Path);
        var logPath = pathProvider.GetDailyLogPath(date);

        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath, "[]");

        using var logger = new DailyFileLogger(new JsonLogFormatter(), pathProvider);

        var entry = new EasySave.Log.LogEntry(
            new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc),
            "JobAppend",
            LogEventType.TransferFile,
            "src",
            "dst",
            10,
            11);

        logger.Write(entry);

        var entries = ReadLogEntries(logPath);
        Assert.Single(entries);
        Assert.Equal("JobAppend", entries[0].BackupName);
    }

    private static List<EasySave.Log.LogEntry> ReadLogEntries(string path)
    {
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<EasySave.Log.LogEntry>>(json, options) ?? [];
    }

    private sealed class TestPathProvider : IPathProvider
    {
        private readonly string _root;

        public TestPathProvider(string root)
        {
            _root = root;
        }

        public DateTime? LastRequestedDate { get; private set; }

        public string GetDailyLogPath(DateTime date)
        {
            LastRequestedDate = date;
            return Path.Combine(_root, $"{date:yyyy-MM-dd}.json");
        }

        public string GetStatePath()
            => Path.Combine(_root, "state.json");

        public string GetJobsConfigPath()
            => Path.Combine(_root, "jobs.json");

        public string GetUserPreferencesPath()
            => Path.Combine(_root, "prefs.json");

        public void SetLogDirectoryOverride(string? directory)
        {
            // Not used in tests.
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EasyLogTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
