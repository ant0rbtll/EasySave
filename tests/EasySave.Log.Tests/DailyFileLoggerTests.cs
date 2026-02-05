using System.Text.Json;
using EasyLog;
using EasySave.Configuration;
using EasySave.Log;

namespace EasySave.Log.Tests;

public class DailyFileLoggerTests
{
    [Fact]
    public void Write_CreatesLogFileAndAppendsEntries()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        using var logger = new DailyFileLogger(
            new JsonLogFormatter(),
            pathProvider,
            mutexName: $"EasySaveLogTests_{Guid.NewGuid():N}");

        var timestamp = new DateTime(2026, 2, 5, 10, 11, 12, DateTimeKind.Unspecified);
        var entry1 = new LogEntry(
            timestamp,
            "JobA",
            LogEventType.TransferFile,
            "  C:/Source  ",
            "D:/Dest",
            12,
            34);
        var entry2 = entry1 with { BackupName = "JobB", FileSizeBytes = 99 };

        logger.Write(entry1);
        logger.Write(entry2);

        var path = pathProvider.GetDailyLogPath(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc).Date);
        Assert.True(File.Exists(path));

        var entries = ReadLogEntries(path);
        Assert.Equal(2, entries.Count);

        var logged = entries.Single(e => e.BackupName == "JobA");

        var expectedTimestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
        Assert.Equal(expectedTimestamp, logged.Timestamp);
        Assert.Equal(DateTimeKind.Utc, logged.Timestamp.Kind);
        Assert.Equal("C:\\Source", logged.SourcePathUNC);
        Assert.Equal("D:\\Dest", logged.DestinationPathUNC);
    }

    [Fact]
    public void Write_ThrowsOnNullEntry()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        using var logger = new DailyFileLogger(
            new JsonLogFormatter(),
            pathProvider,
            mutexName: $"EasySaveLogTests_{Guid.NewGuid():N}");

        Assert.Throws<ArgumentNullException>(() => logger.Write(null!));
    }

    [Fact]
    public void Write_AppendsToExistingEmptyArrayFile()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        using var logger = new DailyFileLogger(
            new JsonLogFormatter(),
            pathProvider,
            mutexName: $"EasySaveLogTests_{Guid.NewGuid():N}");

        var date = new DateTime(2026, 2, 5);
        var path = pathProvider.GetDailyLogPath(date);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "[]");

        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc),
            "JobEmpty",
            LogEventType.CreateDirectory,
            "src",
            "dst",
            0,
            1);

        logger.Write(entry);

        var entries = ReadLogEntries(path);
        Assert.Single(entries);
        Assert.Equal("JobEmpty", entries[0].BackupName);
    }

    [Fact]
    public void Write_AppendsToExistingArrayWithEntry()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        var formatter = new JsonLogFormatter();
        using var logger = new DailyFileLogger(
            formatter,
            pathProvider,
            mutexName: $"EasySaveLogTests_{Guid.NewGuid():N}");

        var date = new DateTime(2026, 2, 5);
        var path = pathProvider.GetDailyLogPath(date);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var first = new LogEntry(
            new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc),
            "JobFirst",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);
        var firstJson = formatter.Format(first);

        File.WriteAllText(path, "[\n" + IndentBlock(firstJson, 2) + "\n]\n");

        var second = first with { BackupName = "JobSecond", FileSizeBytes = 42 };
        logger.Write(second);

        var entries = ReadLogEntries(path);
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.BackupName == "JobFirst");
        Assert.Contains(entries, e => e.BackupName == "JobSecond");
    }

    [Fact]
    public void Write_CreatesValidArrayWhenFileIsEmpty()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        using var logger = new DailyFileLogger(
            new JsonLogFormatter(),
            pathProvider,
            mutexName: $"EasySaveLogTests_{Guid.NewGuid():N}");

        var date = new DateTime(2026, 2, 5);
        var path = pathProvider.GetDailyLogPath(date);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Empty);

        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 11, 0, 0, DateTimeKind.Utc),
            "JobEmptyFile",
            LogEventType.TransferFile,
            "src",
            "dst",
            2,
            3);

        logger.Write(entry);

        var entries = ReadLogEntries(path);
        Assert.Single(entries);
        Assert.Equal("JobEmptyFile", entries[0].BackupName);
    }

    [Fact]
    public void Write_ThrowsWhenFileIsCorrupted()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        var formatter = new JsonLogFormatter();
        using var logger = new DailyFileLogger(
            formatter,
            pathProvider,
            mutexName: $"EasySaveLogTests_{Guid.NewGuid():N}");

        var date = new DateTime(2026, 2, 5);
        var path = pathProvider.GetDailyLogPath(date);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc),
            "JobBroken",
            LogEventType.Error,
            "src",
            "dst",
            0,
            0);
        var entryJson = formatter.Format(entry);

        File.WriteAllText(path, "[\n" + IndentBlock(entryJson, 2) + "\n");

        Assert.Throws<InvalidOperationException>(() => logger.Write(entry));
    }

    [Fact]
    public void Write_NormalizesSlashesAndTrimsWhitespace()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        using var logger = new DailyFileLogger(
            new JsonLogFormatter(),
            pathProvider,
            mutexName: $"EasySaveLogTests_{Guid.NewGuid():N}");

        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 12, 0, 0, DateTimeKind.Utc),
            "JobPaths",
            LogEventType.TransferFile,
            "  /a/b  ",
            "  c:/d/e  ",
            1,
            1);

        logger.Write(entry);

        var path = pathProvider.GetDailyLogPath(entry.Timestamp.Date);
        var logged = ReadLogEntries(path).Single();

        Assert.Equal("\\a\\b", logged.SourcePathUNC);
        Assert.Equal("c:\\d\\e", logged.DestinationPathUNC);
    }

    [Fact]
    public void Write_ConvertsLocalTimeToUtc()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        using var logger = new DailyFileLogger(
            new JsonLogFormatter(),
            pathProvider,
            mutexName: $"EasySaveLogTests_{Guid.NewGuid():N}");

        var local = DateTime.SpecifyKind(new DateTime(2026, 2, 5, 8, 0, 0), DateTimeKind.Local);
        var entry = new LogEntry(
            local,
            "JobLocal",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);

        logger.Write(entry);

        var path = pathProvider.GetDailyLogPath(local.ToUniversalTime().Date);
        var logged = ReadLogEntries(path).Single();

        Assert.Equal(local.ToUniversalTime(), logged.Timestamp);
        Assert.Equal(DateTimeKind.Utc, logged.Timestamp.Kind);
    }

    [Fact]
    public void Write_ThrowsTimeoutWhenMutexIsHeld()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        var mutexName = $"EasySaveLogTests_{Guid.NewGuid():N}";

        using var mutex = new Mutex(false, mutexName);
        Assert.True(mutex.WaitOne(TimeSpan.FromMilliseconds(100)));

        using var logger = new DailyFileLogger(
            new JsonLogFormatter(),
            pathProvider,
            mutexName: mutexName);

        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 12, 0, 0, DateTimeKind.Utc),
            "JobMutex",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);

        var ex = Assert.Throws<TimeoutException>(() => logger.Write(entry));
        Assert.Contains("mutex", ex.Message, StringComparison.OrdinalIgnoreCase);

        mutex.ReleaseMutex();
    }

    [Fact]
    public void Write_UsesUtcDateForDailyLogPath()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        using var logger = new DailyFileLogger(
            new JsonLogFormatter(),
            pathProvider,
            mutexName: $"EasySaveLogTests_{Guid.NewGuid():N}");

        var local = DateTime.SpecifyKind(new DateTime(2026, 2, 5, 23, 30, 0), DateTimeKind.Local);
        var entry = new LogEntry(
            local,
            "JobDate",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);

        logger.Write(entry);

        var expectedPath = pathProvider.GetDailyLogPath(local.ToUniversalTime().Date);
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public void Write_DoesNotWriteUtf8Bom()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        using var logger = new DailyFileLogger(
            new JsonLogFormatter(),
            pathProvider,
            mutexName: $"EasySaveLogTests_{Guid.NewGuid():N}");

        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 12, 0, 0, DateTimeKind.Utc),
            "JobBom",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);

        logger.Write(entry);

        var path = pathProvider.GetDailyLogPath(entry.Timestamp.Date);
        var bytes = File.ReadAllBytes(path);
        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        Assert.False(hasBom);
    }

    [Fact]
    public void Write_AppendsManyEntriesKeepsValidJsonArray()
    {
        using var tempDir = new TempDirectory();
        var pathProvider = new TestPathProvider(tempDir.Path);
        using var logger = new DailyFileLogger(
            new JsonLogFormatter(),
            pathProvider,
            mutexName: $"EasySaveLogTests_{Guid.NewGuid():N}");

        var ts = new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 50; i++)
        {
            var entry = new LogEntry(
                ts.AddSeconds(i),
                $"Job_{i}",
                LogEventType.TransferFile,
                "src",
                "dst",
                i,
                i);
            logger.Write(entry);
        }

        var path = pathProvider.GetDailyLogPath(ts.Date);
        var entries = ReadLogEntries(path);
        Assert.Equal(50, entries.Count);
    }

    private static List<LogEntry> ReadLogEntries(string path)
    {
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<LogEntry>>(json, options) ?? [];
    }

    private static string IndentBlock(string text, int spaces)
    {
        var indent = new string(' ', spaces);
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
            lines[i] = indent + lines[i];

        return string.Join(Environment.NewLine, lines);
    }

    private sealed class TestPathProvider : IPathProvider
    {
        private readonly string _baseDir;

        public TestPathProvider(string baseDir)
        {
            _baseDir = baseDir;
        }

        public string GetDailyLogPath(DateTime date)
            => Path.Combine(_baseDir, $"{date:yyyy-MM-dd}.json");

        public string GetStatePath() => Path.Combine(_baseDir, "state.json");

        public string GetJobsConfigPath() => Path.Combine(_baseDir, "jobs.json");

        public string GetUserPreferencesPath() => Path.Combine(_baseDir, "prefs.json");

        public void SetLogDirectoryOverride(string? directory)
        {
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EasySaveLogTests", Guid.NewGuid().ToString("N"));
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
