using System.Text.Json;
using EasyLog;
using EasySave.Configuration;
using EasySave.Log;
using Moq;

namespace EasySave.Log.Tests;

public class DailyFileLoggerTests
{
    [Fact]
    public void Write_CreatesLogFileAndAppendsEntries()
    {
        using var tempDir = new TempDirectory();
        var timestamp = new DateTime(2026, 2, 5, 10, 11, 12, DateTimeKind.Unspecified);
        var expectedDate = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc).Date;
        var logPath = GetLogPath(tempDir, expectedDate);

        using var logger = CreateLogger(tempDir, logPath, out var pathProviderMock, out var formatterMock);

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

        Assert.True(File.Exists(logPath));

        var entries = ReadLogEntries(logPath);
        Assert.Equal(2, entries.Count);

        var logged = entries.Single(e => e.BackupName == "JobA");

        var expectedTimestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
        Assert.Equal(expectedTimestamp, logged.Timestamp);
        Assert.Equal(DateTimeKind.Utc, logged.Timestamp.Kind);
        Assert.Equal("C:\\Source", logged.SourcePathUNC);
        Assert.Equal("D:\\Dest", logged.DestinationPathUNC);

        pathProviderMock.Verify(p => p.GetDailyLogPath(expectedDate, "json"), Times.Exactly(2));
        formatterMock.Verify(f => f.Format(It.Is<LogEntry>(e =>
            e.Timestamp == expectedTimestamp &&
            e.SourcePathUNC == "C:\\Source" &&
            e.DestinationPathUNC == "D:\\Dest")), Times.Exactly(2));
    }

    [Fact]
    public void Write_ThrowsOnNullEntry()
    {
        using var tempDir = new TempDirectory();
        var logPath = GetLogPath(tempDir, new DateTime(2026, 2, 5));

        using var logger = CreateLogger(tempDir, logPath, out _, out _);

        Assert.Throws<ArgumentNullException>(() => logger.Write(null!));
    }

    [Fact]
    public void Write_AppendsToExistingEmptyArrayFile()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var logPath = GetLogPath(tempDir, date);
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath, "[\n]");

        using var logger = CreateLogger(tempDir, logPath, out _, out _);

        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc),
            "JobEmpty",
            LogEventType.CreateDirectory,
            "src",
            "dst",
            0,
            1);

        logger.Write(entry);

        var entries = ReadLogEntries(logPath);
        Assert.Single(entries);
        Assert.Equal("JobEmpty", entries[0].BackupName);
    }

    [Fact]
    public void Write_AppendsToExistingArrayWithEntry()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var logPath = GetLogPath(tempDir, date);
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        var first = new LogEntry(
            new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc),
            "JobFirst",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);
        var firstJson = SerializeEntry(first);

        File.WriteAllText(logPath, "[\n" + IndentBlock(firstJson, 2) + "\n]\n");

        using var logger = CreateLogger(tempDir, logPath, out _, out _);

        var second = first with { BackupName = "JobSecond", FileSizeBytes = 42 };
        logger.Write(second);

        var entries = ReadLogEntries(logPath);
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.BackupName == "JobFirst");
        Assert.Contains(entries, e => e.BackupName == "JobSecond");
    }

    [Fact]
    public void Write_CreatesValidArrayWhenFileIsEmpty()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var logPath = GetLogPath(tempDir, date);
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath, string.Empty);

        using var logger = CreateLogger(tempDir, logPath, out _, out _);

        var entry = new LogEntry(
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
    public void Write_HandlesCorruptedFile()
    {
        // The new implementation doesn't throw for corrupted files
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var logPath = GetLogPath(tempDir, date);
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc),
            "JobBroken",
            LogEventType.Error,
            "src",
            "dst",
            0,
            0);
        var entryJson = SerializeEntry(entry);

        // Write a corrupted file (incomplete JSON array)
        File.WriteAllText(logPath, "[\n" + IndentBlock(entryJson, 2) + "\n");

        using var logger = CreateLogger(tempDir, logPath, out _, out _);

        // Should not throw - will append entry despite corruption
        logger.Write(entry);
        
        // Verify the entry was written
        Assert.True(File.Exists(logPath));
        var content = File.ReadAllText(logPath);
        Assert.Contains("JobBroken", content);
    }

    [Fact]
    public void Write_NormalizesSlashesAndTrimsWhitespace()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var logPath = GetLogPath(tempDir, date);

        using var logger = CreateLogger(tempDir, logPath, out _, out _);

        var entry = new LogEntry(
            new DateTime(2026, 2, 5, 12, 0, 0, DateTimeKind.Utc),
            "JobPaths",
            LogEventType.TransferFile,
            "  /a/b  ",
            "  c:/d/e  ",
            1,
            1);

        logger.Write(entry);

        var logged = ReadLogEntries(logPath).Single();

        Assert.Equal("\\a\\b", logged.SourcePathUNC);
        Assert.Equal("c:\\d\\e", logged.DestinationPathUNC);
    }

    [Fact]
    public void Write_ConvertsLocalTimeToUtc()
    {
        using var tempDir = new TempDirectory();
        var local = DateTime.SpecifyKind(new DateTime(2026, 2, 5, 8, 0, 0), DateTimeKind.Local);
        var logPath = GetLogPath(tempDir, local.ToUniversalTime().Date);

        using var logger = CreateLogger(tempDir, logPath, out var pathProviderMock, out _);

        var entry = new LogEntry(
            local,
            "JobLocal",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);

        logger.Write(entry);

        var logged = ReadLogEntries(logPath).Single();

        Assert.Equal(local.ToUniversalTime(), logged.Timestamp);
        Assert.Equal(DateTimeKind.Utc, logged.Timestamp.Kind);
        pathProviderMock.Verify(p => p.GetDailyLogPath(local.ToUniversalTime().Date, "json"), Times.Once);
    }

    [Fact]
    public void Write_ThrowsTimeoutWhenMutexIsHeld()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var logPath = GetLogPath(tempDir, date);
        var mutexName = $"EasySaveLogTests_{Guid.NewGuid():N}";

        using var acquired = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        var holder = new Thread(() =>
        {
            using var mutex = new Mutex(false, mutexName);
            mutex.WaitOne();
            acquired.Set();
            release.Wait();
            mutex.ReleaseMutex();
        })
        { IsBackground = true };

        holder.Start();
        Assert.True(acquired.Wait(TimeSpan.FromSeconds(2)));

        using var logger = CreateLogger(tempDir, logPath, out _, out _, mutexName: mutexName);

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

        release.Set();
        holder.Join(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Write_UsesUtcDateForDailyLogPath()
    {
        using var tempDir = new TempDirectory();
        var local = DateTime.SpecifyKind(new DateTime(2026, 2, 5, 23, 30, 0), DateTimeKind.Local);
        var expectedDate = local.ToUniversalTime().Date;
        var logPath = GetLogPath(tempDir, expectedDate);

        using var logger = CreateLogger(tempDir, logPath, out var pathProviderMock, out _);

        var entry = new LogEntry(
            local,
            "JobDate",
            LogEventType.TransferFile,
            "src",
            "dst",
            1,
            1);

        logger.Write(entry);

        Assert.True(File.Exists(logPath));
        pathProviderMock.Verify(p => p.GetDailyLogPath(expectedDate, "json"), Times.Once);
    }

    [Fact]
    public void Write_DoesNotWriteUtf8Bom()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var logPath = GetLogPath(tempDir, date);

        using var logger = CreateLogger(tempDir, logPath, out _, out _);

        var entry = new LogEntry(
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
    public void Write_AppendsManyEntriesKeepsValidJsonArray()
    {
        using var tempDir = new TempDirectory();
        var date = new DateTime(2026, 2, 5);
        var logPath = GetLogPath(tempDir, date);

        using var logger = CreateLogger(tempDir, logPath, out _, out _);

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

        var entries = ReadLogEntries(logPath);
        Assert.Equal(50, entries.Count);
    }

    private static DailyFileLogger CreateLogger(
        TempDirectory tempDir,
        string logPath,
        out Mock<IPathProvider> pathProviderMock,
        out Mock<ILogFormatter> formatterMock,
        string? mutexName = null)
    {
        pathProviderMock = new Mock<IPathProvider>();
        pathProviderMock
            .Setup(p => p.GetDailyLogPath(It.IsAny<DateTime>(), It.IsAny<string>()))
            .Returns(logPath);

        formatterMock = new Mock<ILogFormatter>();
        formatterMock
            .Setup(f => f.Format(It.IsAny<LogEntry>()))
            .Returns<LogEntry>(SerializeEntry);
        formatterMock
            .Setup(f => f.GetFileHeader())
            .Returns("[");
        formatterMock
            .Setup(f => f.GetFileFooter())
            .Returns("]");
        formatterMock
            .Setup(f => f.GetEntrySeparator())
            .Returns(",");
        formatterMock
            .Setup(f => f.GetIndentSpaces())
            .Returns(2);

        return new DailyFileLogger(
            formatterMock.Object,
            pathProviderMock.Object,
            mutexName: mutexName ?? $"EasySaveLogTests_{Guid.NewGuid():N}");
    }

    private static string GetLogPath(TempDirectory tempDir, DateTime date)
        => Path.Combine(tempDir.Path, $"{date:yyyy-MM-dd}.json");

    private static List<LogEntry> ReadLogEntries(string path)
    {
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<LogEntry>>(json, options) ?? [];
    }

    private static string SerializeEntry(LogEntry entry)
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
