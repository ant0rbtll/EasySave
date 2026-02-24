using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using EasySave.Application.Readers;
using EasySave.Application.Services;
using EasySave.Configuration;
using EasySave.Core;
using EasySave.Log;

namespace EasySave.Application.Tests;

public class LogNavigationServiceTests
{
    [Fact]
    public void GetJobsByDate_GroupsRunsAcrossFormats()
    {
        using var temp = new TempDirectory();
        var date = new DateOnly(2026, 2, 12);

        WriteJson(Path.Combine(temp.LogsDirectory, "2026-02-12.json"),
        [
            CreateEntry("2026-02-12T10:00:00Z", 1, "job-1", LogEventType.StartBackup),
            CreateEntry("2026-02-12T10:00:01Z", 1, "job-1", LogEventType.TransferFile),
            CreateEntry("2026-02-12T10:00:02Z", 2, "job-2", LogEventType.StartBackup),
            CreateEntry("2026-02-12T10:00:03Z", 2, "job-2", LogEventType.TransferFile),
            CreateEntry("2026-02-12T10:00:04Z", 1, "job-1", LogEventType.TransferFile),
            CreateEntry("2026-02-12T10:00:05Z", 1, "job-1", LogEventType.EndBackup, transferTimeMs: 500, fileSizeBytes: 1000),
            CreateEntry("2026-02-12T10:00:06Z", 2, "job-2", LogEventType.EndBackup, transferTimeMs: 700, fileSizeBytes: 1200)
        ]);

        WriteXml(Path.Combine(temp.LogsDirectory, "2026-02-12.xml"),
        [
            CreateEntry("2026-02-12T11:00:00Z", 1, "job-1", LogEventType.StartBackup),
            CreateEntry("2026-02-12T11:00:01Z", 1, "job-1", LogEventType.TransferFile)
        ]);

        var service = CreateService(temp.LogsDirectory);

        var jobs = service.GetJobsByDate(date);

        Assert.Equal(2, jobs.Count);
        Assert.Contains(jobs, static j => j.BackupId == 1 && j.BackupName == "job-1" && j.RunCount == 2);
        Assert.Contains(jobs, static j => j.BackupId == 2 && j.BackupName == "job-2" && j.RunCount == 1);
    }

    [Fact]
    public void GetRunsByDateAndBackupId_MarksCompletedAndInProgress()
    {
        using var temp = new TempDirectory();
        var date = new DateOnly(2026, 2, 12);

        WriteJson(Path.Combine(temp.LogsDirectory, "2026-02-12.json"),
        [
            CreateEntry("2026-02-12T10:00:00Z", 1, "job-1", LogEventType.StartBackup),
            CreateEntry("2026-02-12T10:00:05Z", 1, "job-1", LogEventType.EndBackup, transferTimeMs: 500, fileSizeBytes: 2048)
        ]);

        WriteXml(Path.Combine(temp.LogsDirectory, "2026-02-12.xml"),
        [
            CreateEntry("2026-02-12T11:00:00Z", 1, "job-1", LogEventType.StartBackup),
            CreateEntry("2026-02-12T11:00:01Z", 1, "job-1", LogEventType.TransferFile)
        ]);

        var service = CreateService(temp.LogsDirectory);

        var runs = service.GetRunsByDateAndBackupId(date, 1);

        Assert.Equal(2, runs.Count);
        Assert.Equal(LogRunStatus.InProgress, runs[0].Status);
        Assert.Null(runs[0].EndTimestamp);
        Assert.Null(runs[0].TotalDurationMs);
        Assert.Null(runs[0].TotalSizeBytes);

        Assert.Equal(LogRunStatus.Completed, runs[1].Status);
        Assert.Equal(500, runs[1].TotalDurationMs);
        Assert.Equal(2048, runs[1].TotalSizeBytes);
    }

    [Fact]
    public void GetRunsByDateAndBackupId_MarksErrorWhenTerminalEventIsError()
    {
        using var temp = new TempDirectory();
        var date = new DateOnly(2026, 2, 12);

        WriteJson(Path.Combine(temp.LogsDirectory, "2026-02-12.json"),
        [
            CreateEntry("2026-02-12T10:00:00Z", 3, "job-3", LogEventType.StartBackup),
            CreateEntry("2026-02-12T10:00:01Z", 3, "job-3", LogEventType.TransferFile),
            CreateEntry("2026-02-12T10:00:02Z", 3, "job-3", LogEventType.Error, transferTimeMs: 1234)
        ]);

        var service = CreateService(temp.LogsDirectory);

        var runs = service.GetRunsByDateAndBackupId(date, 3);

        var run = Assert.Single(runs);
        Assert.Equal(LogRunStatus.Error, run.Status);
        Assert.NotNull(run.EndTimestamp);
        Assert.Equal(1234, run.TotalDurationMs);
        Assert.Null(run.TotalSizeBytes);
    }

    [Fact]
    public void GetRunsByDateAndBackupId_WhenLastEventIsBusinessSoftwareDetected_ShouldMarkBlocked()
    {
        using var temp = new TempDirectory();
        var date = new DateOnly(2026, 2, 12);

        WriteJson(Path.Combine(temp.LogsDirectory, "2026-02-12.json"),
        [
            CreateEntry("2026-02-12T10:00:00Z", 9, "job-9", LogEventType.StartBackup),
            CreateEntry("2026-02-12T10:00:01Z", 9, "job-9", LogEventType.BusinessSoftwareDetected)
        ]);

        var service = CreateService(temp.LogsDirectory);

        var runs = service.GetRunsByDateAndBackupId(date, 9);

        var run = Assert.Single(runs);
        Assert.Equal(LogRunStatus.Blocked, run.Status);
        Assert.Null(run.EndTimestamp);
    }

    [Fact]
    public void GetRunsByDateAndBackupId_MarksStoppedWhenTerminalEventIsStopped()
    {
        using var temp = new TempDirectory();
        var date = new DateOnly(2026, 2, 12);

        WriteJson(Path.Combine(temp.LogsDirectory, "2026-02-12.json"),
        [
            CreateEntry("2026-02-12T10:00:00Z", 4, "job-4", LogEventType.StartBackup),
            CreateEntry("2026-02-12T10:00:01Z", 4, "job-4", LogEventType.TransferFile),
            CreateEntry("2026-02-12T10:00:02Z", 4, "job-4", LogEventType.Stopped, transferTimeMs: 2345)
        ]);

        var service = CreateService(temp.LogsDirectory);

        var runs = service.GetRunsByDateAndBackupId(date, 4);

        var run = Assert.Single(runs);
        Assert.Equal(LogRunStatus.Stopped, run.Status);
        Assert.NotNull(run.EndTimestamp);
        Assert.Equal(2345, run.TotalDurationMs);
        Assert.Null(run.TotalSizeBytes);
    }

    [Fact]
    public void GetRunsByDateAndBackupId_MarksPausedWhenLastEventIsPauseAction()
    {
        using var temp = new TempDirectory();
        var date = new DateOnly(2026, 2, 12);

        WriteJson(Path.Combine(temp.LogsDirectory, "2026-02-12.json"),
        [
            CreateEntry("2026-02-12T10:00:00Z", 5, "job-5", LogEventType.StartBackup),
            CreateEntry("2026-02-12T10:00:01Z", 5, "job-5", LogEventType.TransferFile),
            CreateEntry("2026-02-12T10:00:02Z", 5, "job-5", LogEventType.Action, sourcePathUNC: "action_backup_paused_by_user")
        ]);

        var service = CreateService(temp.LogsDirectory);

        var runs = service.GetRunsByDateAndBackupId(date, 5);

        var run = Assert.Single(runs);
        Assert.Equal(LogRunStatus.Paused, run.Status);
        Assert.Null(run.EndTimestamp);
        Assert.Null(run.TotalDurationMs);
        Assert.Null(run.TotalSizeBytes);
    }

    [Fact]
    public void GetEntriesByRun_FiltersByBackupId_WhenLogsAreInterleaved()
    {
        using var temp = new TempDirectory();
        var date = new DateOnly(2026, 2, 12);

        WriteJson(Path.Combine(temp.LogsDirectory, "2026-02-12.json"),
        [
            CreateEntry("2026-02-12T10:00:00Z", 1, "job-1", LogEventType.StartBackup),
            CreateEntry("2026-02-12T10:00:01Z", 1, "job-1", LogEventType.TransferFile),
            CreateEntry("2026-02-12T10:00:02Z", 2, "job-2", LogEventType.StartBackup),
            CreateEntry("2026-02-12T10:00:03Z", 2, "job-2", LogEventType.TransferFile),
            CreateEntry("2026-02-12T10:00:04Z", 1, "job-1", LogEventType.TransferFile),
            CreateEntry("2026-02-12T10:00:05Z", 1, "job-1", LogEventType.EndBackup, transferTimeMs: 500, fileSizeBytes: 1000),
            CreateEntry("2026-02-12T10:00:06Z", 2, "job-2", LogEventType.EndBackup, transferTimeMs: 700, fileSizeBytes: 1200)
        ]);

        var service = CreateService(temp.LogsDirectory);
        var runs = service.GetRunsByDateAndBackupId(date, 1);
        var completed = Assert.Single(runs.Where(static r => r.Status == LogRunStatus.Completed));

        var entries = service.GetEntriesByRun(date, completed.RunId);

        Assert.Equal(4, entries.Count);
        Assert.All(entries, static entry => Assert.Equal(1, entry.BackupId));
        Assert.Contains(entries, static entry => entry.EventType == LogEventType.StartBackup);
        Assert.Contains(entries, static entry => entry.EventType == LogEventType.EndBackup);
    }

    [Fact]
    public void Cache_Invalidates_WhenFileSignatureChanges()
    {
        using var temp = new TempDirectory();
        var date = new DateOnly(2026, 2, 12);
        string jsonPath = Path.Combine(temp.LogsDirectory, "2026-02-12.json");

        WriteJson(jsonPath,
        [
            CreateEntry("2026-02-12T10:00:00Z", 1, "job-1", LogEventType.StartBackup),
            CreateEntry("2026-02-12T10:00:05Z", 1, "job-1", LogEventType.EndBackup, transferTimeMs: 500, fileSizeBytes: 512)
        ]);

        var service = CreateService(temp.LogsDirectory);
        var firstRuns = service.GetRunsByDateAndBackupId(date, 1);
        Assert.Single(firstRuns);
        Assert.Equal(LogRunStatus.Completed, firstRuns[0].Status);

        File.AppendAllText(
            jsonPath,
            """
            
            """);
        WriteJson(jsonPath,
        [
            CreateEntry("2026-02-12T10:00:00Z", 1, "job-1", LogEventType.StartBackup),
            CreateEntry("2026-02-12T10:00:05Z", 1, "job-1", LogEventType.EndBackup, transferTimeMs: 500, fileSizeBytes: 512),
            CreateEntry("2026-02-12T11:00:00Z", 1, "job-1", LogEventType.StartBackup)
        ]);

        var secondRuns = service.GetRunsByDateAndBackupId(date, 1);
        Assert.Equal(2, secondRuns.Count);
        Assert.Contains(secondRuns, static run => run.Status == LogRunStatus.InProgress);
    }

    private static LogNavigationService CreateService(string logsDirectory)
    {
        return new LogNavigationService(
            new TestPathProvider(logsDirectory),
            [new JsonLogReader(), new XmlLogReader()]);
    }

    private static LogEntry CreateEntry(
        string timestampUtc,
        int backupId,
        string backupName,
        LogEventType eventType,
        string sourcePathUNC = "\\\\src",
        long transferTimeMs = 0,
        long fileSizeBytes = 0)
    {
        return new LogEntry(
            DateTime.Parse(timestampUtc),
            backupId,
            backupName,
            eventType,
            sourcePathUNC,
            "\\\\dst",
            fileSizeBytes,
            transferTimeMs,
            0);
    }

    private static void WriteJson(string filePath, IReadOnlyList<LogEntry> entries)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) }
        };

        string json = JsonSerializer.Serialize(entries, options);
        File.WriteAllText(filePath, json);
    }

    private static void WriteXml(string filePath, IReadOnlyList<LogEntry> entries)
    {
        var document = new XDocument(
            new XElement("Logs",
                entries.Select(entry => new XElement("LogEntry",
                    new XElement("Timestamp", entry.Timestamp.ToString("o")),
                    new XElement("BackupName", entry.BackupName),
                    new XElement("BackupId", entry.BackupId),
                    new XElement("EventType", entry.EventType.ToString()),
                    new XElement("SourcePathUNC", entry.SourcePathUNC),
                    new XElement("DestinationPathUNC", entry.DestinationPathUNC),
                    new XElement("FileSizeBytes", entry.FileSizeBytes),
                    new XElement("TransferTimeMs", entry.TransferTimeMs),
                    new XElement("EncryptionTimeMs", entry.EncryptionTimeMs)))));

        File.WriteAllText(filePath, document.ToString());
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
            RootDirectory = Path.Combine(Path.GetTempPath(), "easysave-log-navigation-tests", Guid.NewGuid().ToString("N"));
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
