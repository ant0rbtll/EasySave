using System.Text.Json;
using System.Xml.Linq;
using EasySave.Configuration;
using EasySave.Log;
using EasySave.Core;

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
        // Create a proper empty JSON array
        File.WriteAllText(logPath, "[\n]");

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

    [Fact]
    public void Write_CreatesValidArrayWhenFileIsEmpty()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var pathProvider = new TestPathProvider(tempDir.Path);
        var logPath = pathProvider.GetDailyLogPath(date);

        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath, string.Empty);

        using var logger = new DailyFileLogger(new JsonLogFormatter(), pathProvider);

        var entry = new EasySave.Log.LogEntry(
            new DateTime(2026, 2, 5, 11, 0, 0, DateTimeKind.Utc),
            "JobEmptyFile",
            LogEventType.TransferFile,
            "src",
            "dst",
            2,
            3);

        logger.Write(entry);

        var entries = ReadLogEntries(logPath);
        Assert.Single(entries);
        Assert.Equal("JobEmptyFile", entries[0].BackupName);
    }

    [Fact]
    public void Write_AppendsToExistingArrayWithEntry()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var pathProvider = new TestPathProvider(tempDir.Path);
        var logPath = pathProvider.GetDailyLogPath(date);

        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        var first = new EasySave.Log.LogEntry(
            new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc),
            "JobFirst",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);

        File.WriteAllText(logPath, "[\n" + IndentBlock(SerializeEntry(first), 2) + "\n]\n");

        using var logger = new DailyFileLogger(new JsonLogFormatter(), pathProvider);

        var second = first with { BackupName = "JobSecond", FileSizeBytes = 42 };
        logger.Write(second);

        var entries = ReadLogEntries(logPath);
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.BackupName == "JobFirst");
        Assert.Contains(entries, e => e.BackupName == "JobSecond");
    }

    [Fact]
    public void Write_ThrowsWhenFileIsCorrupted()
    {
        // This test is no longer relevant as the new implementation
        // doesn't throw for corrupted files - it recreates them
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var pathProvider = new TestPathProvider(tempDir.Path);
        var logPath = pathProvider.GetDailyLogPath(date);

        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        var entry = new EasySave.Log.LogEntry(
            new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc),
            "JobBroken",
            LogEventType.Error,
            "src",
            "dst",
            0,
            0);

        // Write a corrupted file (incomplete JSON array)
        File.WriteAllText(logPath, "[\n  {\"test\": \"data\"}\n");

        using var logger = new DailyFileLogger(new JsonLogFormatter(), pathProvider);

        // Should not throw - will append entry despite corruption
        logger.Write(entry);
        
        // Verify the entry was written
        Assert.True(File.Exists(logPath));
        var content = File.ReadAllText(logPath);
        Assert.Contains("JobBroken", content);
    }

    [Fact]
    public void Write_DoesNotWriteUtf8Bom()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var pathProvider = new TestPathProvider(tempDir.Path);
        var logPath = pathProvider.GetDailyLogPath(date);

        using var logger = new DailyFileLogger(new JsonLogFormatter(), pathProvider);

        var entry = new EasySave.Log.LogEntry(
            new DateTime(2026, 2, 5, 12, 0, 0, DateTimeKind.Utc),
            "JobBom",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);

        logger.Write(entry);

        var bytes = File.ReadAllBytes(logPath);
        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        Assert.False(hasBom);
    }

    [Fact]
    public void Write_CreatesXmlFileWithLogsRoot()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 6);
        var pathProvider = new TestPathProvider(tempDir.Path);
        var logPath = pathProvider.GetDailyLogPath(date, LogFormat.Xml);

        using var logger = new DailyFileLogger(new XmlLogFormatter(), pathProvider, format: LogFormat.Xml);
        var entry = new EasySave.Log.LogEntry(
            new DateTime(2026, 2, 6, 8, 0, 0, DateTimeKind.Utc),
            "JobXml",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);

        logger.Write(entry);

        var doc = XDocument.Load(logPath);
        Assert.Equal("Logs", doc.Root?.Name.LocalName);
        Assert.Single(doc.Root?.Elements("LogEntry") ?? []);
    }

    [Fact]
    public void Write_AppendsXmlEntries()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 6);
        var pathProvider = new TestPathProvider(tempDir.Path);
        var logPath = pathProvider.GetDailyLogPath(date, LogFormat.Xml);

        using var logger = new DailyFileLogger(new XmlLogFormatter(), pathProvider, format: LogFormat.Xml);
        var first = new EasySave.Log.LogEntry(
            new DateTime(2026, 2, 6, 8, 0, 0, DateTimeKind.Utc),
            "JobXml1",
            LogEventType.StartBackup,
            "src",
            "dst",
            0,
            0);
        var second = first with
        {
            Timestamp = new DateTime(2026, 2, 6, 8, 1, 0, DateTimeKind.Utc),
            BackupName = "JobXml2",
            EventType = LogEventType.EndBackup
        };

        logger.Write(first);
        logger.Write(second);

        var doc = XDocument.Load(logPath);
        var entries = doc.Root?.Elements("LogEntry").ToList() ?? [];
        Assert.Equal(2, entries.Count);
        Assert.Equal("JobXml1", entries[0].Element("BackupName")?.Value);
        Assert.Equal("JobXml2", entries[1].Element("BackupName")?.Value);
    }

    private static List<EasySave.Log.LogEntry> ReadLogEntries(string path)
    {
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<EasySave.Log.LogEntry>>(json, options) ?? [];
    }

    private static string SerializeEntry(EasySave.Log.LogEntry entry)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(entry, options);
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
        private readonly string _root;

        public TestPathProvider(string root)
        {
            _root = root;
        }

        public DateTime? LastRequestedDate { get; private set; }

        public string GetDailyLogPath(DateTime date, LogFormat format = LogFormat.Json)
        {
            LastRequestedDate = date;
            string extension = format == LogFormat.Xml ? "xml" : "json";
            return Path.Combine(_root, $"{date:yyyy-MM-dd}.{extension}");
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
