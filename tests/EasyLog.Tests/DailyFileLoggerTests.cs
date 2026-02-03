using System.Text.Json;
using EasySave.Backup;
using EasySave.Configuration;
using EasySave.Core;
using EasySave.State;
using EasySave.System;

namespace EasyLog.Tests;

public class DailyFileLoggerTests
{
    [Fact]
    public void ExecuteBackup_WritesTodayLogWithRealData()
    {
        var pathProvider = new DefaultPathProvider();
        var logger = new DailyFileLogger(
            new JsonLogFormatter(),
            pathProvider,
            mutexName: $"EasyLogTests_{Guid.NewGuid():N}");

        var fs = new DefaultFileSystem();
        var transfer = new DefaultTransferService(fs);
        var state = new GlobalState
        {
            Entries = []
        };
        var stateWriter = new RealTimeStateWriter(pathProvider, state);
        var engine = new BackupEngine(fs, transfer, stateWriter, logger);

        using var tempDir = new TempDirectory();
        var sourceDir = Path.Combine(tempDir.Path, "source");
        var destDir = Path.Combine(tempDir.Path, "dest");
        Directory.CreateDirectory(sourceDir);

        var sourceFolderA = Path.Combine(sourceDir, "folderA");
        var sourceFolderB = Path.Combine(sourceDir, "folderB");
        Directory.CreateDirectory(sourceFolderA);
        Directory.CreateDirectory(sourceFolderB);

        var sourceFileA = Path.Combine(sourceFolderA, "fileA.txt");
        var sourceFileB = Path.Combine(sourceFolderB, "fileB.txt");

        var contentA = "easylog-backup-test-A";
        var contentB = "easylog-backup-test-B";

        File.WriteAllText(sourceFileA, contentA);
        File.WriteAllText(sourceFileB, contentB);

        var jobName = $"Job_{Guid.NewGuid():N}";
        var job = new BackupJob
        {
            Id = 1,
            Name = jobName,
            Source = sourceDir,
            Destination = destDir,
            Type = BackupType.Complete
        };

        var today = DateTime.Now.Date;

        engine.Execute(job);

        var logPath = pathProvider.GetDailyLogPath(today);
        var items = ReadLogEntries(logPath)
            .Where(e => e.BackupName == jobName)
            .ToList();

        Assert.NotEmpty(items);

        var transferLogs = items.Where(e => e.EventType == LogEventType.TransferFile).ToList();
        Assert.Equal(2, transferLogs.Count);

        var expected = new Dictionary<string, (string Dest, int Size)>
        {
            [NormalizePath(sourceFileA)] = (NormalizePath(Path.Combine(destDir, "folderA", "fileA.txt")), contentA.Length),
            [NormalizePath(sourceFileB)] = (NormalizePath(Path.Combine(destDir, "folderB", "fileB.txt")), contentB.Length)
        };

        foreach (var log in transferLogs)
        {
            Assert.True(expected.TryGetValue(log.SourcePathUNC, out var expectedInfo));
            Assert.Equal(expectedInfo.Dest, log.DestinationPathUNC);
            Assert.Equal(expectedInfo.Size, log.FileSizeBytes);
        }
    }

    private static List<LogEntry> ReadLogEntries(string path)
    {
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<LogEntry>>(json, options) ?? new List<LogEntry>();
    }

    private static string NormalizePath(string path)
        => path.Replace('/', '\\');

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
