using System.Text.Json;
using EasySave.Configuration;
using EasySave.Core;
using EasySave.Log;

namespace EasySave.Application.Tests;

public class LogQueryServiceTests
{
    [Fact]
    public void GetAvailableDates_WhenDirectoryDoesNotExist_ReturnsEmpty()
    {
        string missingDirectory = Path.Combine(Path.GetTempPath(), "easysave-log-query-missing", Guid.NewGuid().ToString("N"));
        var service = CreateService(missingDirectory, [new JsonLogReader()]);

        var result = service.GetAvailableDates(LogFormat.Json);

        Assert.Empty(result);
    }

    [Fact]
    public void GetAvailableDates_FiltersByFormatAndSortsDescending()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.LogsDirectory, "2026-02-09.json"), "[]");
        File.WriteAllText(Path.Combine(temp.LogsDirectory, "2026-02-11.json"), "[]");
        File.WriteAllText(Path.Combine(temp.LogsDirectory, "2026-02-10.xml"), "<Logs />");
        File.WriteAllText(Path.Combine(temp.LogsDirectory, "invalid.json"), "[]");

        var service = CreateService(temp.LogsDirectory, [new JsonLogReader()]);

        var result = service.GetAvailableDates(LogFormat.Json);

        Assert.Equal(
            [new DateOnly(2026, 2, 11), new DateOnly(2026, 2, 9)],
            result);
    }

    [Fact]
    public void GetByDate_WhenFileDoesNotExist_ReturnsEmpty()
    {
        using var temp = new TempDirectory();
        var service = CreateService(temp.LogsDirectory, [new JsonLogReader()]);

        var result = service.GetByDate(new DateOnly(2026, 2, 11), LogFormat.Json);

        Assert.Empty(result);
    }

    [Fact]
    public void GetByDate_WithJsonReader_ReturnsTypedEntries()
    {
        using var temp = new TempDirectory();
        var expected = new LogEntry(
            DateTime.Parse("2026-02-11T13:12:50.8789933Z"),
            "job-json",
            LogEventType.TransferFile,
            "\\\\src",
            "\\\\dst",
            10,
            20,
            30);

        string jsonPath = Path.Combine(temp.LogsDirectory, "2026-02-11.json");
        WriteJson(jsonPath, [expected]);

        var service = CreateService(temp.LogsDirectory, [new JsonLogReader()]);

        var result = service.GetByDate(new DateOnly(2026, 2, 11), LogFormat.Json);

        var actual = Assert.Single(result);
        Assert.Equal(expected.BackupName, actual.BackupName);
        Assert.Equal(expected.EventType, actual.EventType);
        Assert.Equal(expected.SourcePathUNC, actual.SourcePathUNC);
        Assert.Equal(expected.DestinationPathUNC, actual.DestinationPathUNC);
        Assert.Equal(expected.FileSizeBytes, actual.FileSizeBytes);
        Assert.Equal(expected.TransferTimeMs, actual.TransferTimeMs);
        Assert.Equal(expected.EncryptionTimeMs, actual.EncryptionTimeMs);
    }

    [Fact]
    public void GetByDate_WithXmlReader_ReturnsTypedEntries()
    {
        using var temp = new TempDirectory();
        string xmlPath = Path.Combine(temp.LogsDirectory, "2026-02-11.xml");
        File.WriteAllText(
            xmlPath,
            """
            <?xml version="1.0" encoding="utf-8"?>
            <Logs>
              <LogEntry>
                <Timestamp>2026-02-11T13:12:50.8789933Z</Timestamp>
                <BackupName>job-xml</BackupName>
                <EventType>TransferFile</EventType>
                <SourcePathUNC>\\src-xml</SourcePathUNC>
                <DestinationPathUNC>\\dst-xml</DestinationPathUNC>
                <FileSizeBytes>44</FileSizeBytes>
                <TransferTimeMs>55</TransferTimeMs>
                <EncryptionTimeMs>66</EncryptionTimeMs>
              </LogEntry>
            </Logs>
            """);

        var service = CreateService(temp.LogsDirectory, [new XmlLogReader()]);

        var result = service.GetByDate(new DateOnly(2026, 2, 11), LogFormat.Xml);

        var actual = Assert.Single(result);
        Assert.Equal("job-xml", actual.BackupName);
        Assert.Equal(LogEventType.TransferFile, actual.EventType);
        Assert.Equal("\\\\src-xml", actual.SourcePathUNC);
        Assert.Equal("\\\\dst-xml", actual.DestinationPathUNC);
        Assert.Equal(44, actual.FileSizeBytes);
        Assert.Equal(55, actual.TransferTimeMs);
        Assert.Equal(66, actual.EncryptionTimeMs);
    }

    [Fact]
    public void GetByDate_WhenReaderIsMissing_ThrowsNotSupportedException()
    {
        using var temp = new TempDirectory();
        var service = CreateService(temp.LogsDirectory, [new JsonLogReader()]);

        var action = () => service.GetByDate(new DateOnly(2026, 2, 11), LogFormat.Xml);

        Assert.Throws<NotSupportedException>(action);
    }

    private static LogQueryService CreateService(string logsDirectory, IEnumerable<ILogReader> readers)
    {
        return new LogQueryService(new TestPathProvider(logsDirectory), readers);
    }

    private static void WriteJson(string filePath, IReadOnlyList<LogEntry> entries)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string json = JsonSerializer.Serialize(entries, options);
        File.WriteAllText(filePath, json);
    }

    private sealed class TestPathProvider(string logsDirectory) : IPathProvider
    {
        private readonly string _logsDirectory = logsDirectory;

        public string GetDailyLogPath(DateTime date, LogFormat format = LogFormat.Json)
        {
            string extension = format == LogFormat.Xml ? "xml" : "json";
            return Path.Combine(_logsDirectory, $"{date:yyyy-MM-dd}.{extension}");
        }

        public string GetStatePath() => Path.Combine(_logsDirectory, "state.json");

        public string GetJobsConfigPath() => Path.Combine(_logsDirectory, "jobs.json");

        public string GetUserPreferencesPath() => Path.Combine(_logsDirectory, "user-preferences.json");

        public void SetLogDirectoryOverride(string? directory)
        {
        }

        public string ResolveLogsDirectory() => _logsDirectory;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            RootDirectory = Path.Combine(Path.GetTempPath(), "easysave-log-query-tests", Guid.NewGuid().ToString("N"));
            LogsDirectory = Path.Combine(RootDirectory, "logs");
            Directory.CreateDirectory(LogsDirectory);
        }

        public string RootDirectory { get; }

        public string LogsDirectory { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootDirectory))
                {
                    Directory.Delete(RootDirectory, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
