using EasySave.Configuration;
using EasySave.Core;

namespace EasySave.Application.Tests;

public class JsonStateReaderTests
{
    [Fact]
    public void ReadEntries_WhenFileContainsValidJson_ReturnsEntries()
    {
        using var temp = new TempDirectory();
        var statePath = Path.Combine(temp.RootPath, "state.json");
        File.WriteAllText(
            statePath,
            """
            {
              "3": {
                "backupId": 3,
                "timestamp": "2026-02-11T10:30:00Z",
                "status": 0
              }
            }
            """);

        var reader = new JsonStateReader(new TestPathProvider(statePath));

        var entries = reader.ReadEntries();

        Assert.True(entries.ContainsKey(3));
        Assert.Equal(3, entries[3].BackupId);
    }

    [Fact]
    public void ReadEntries_WhenStatusIsString_ReturnsEntries()
    {
        using var temp = new TempDirectory();
        var statePath = Path.Combine(temp.RootPath, "state.json");
        File.WriteAllText(
            statePath,
            """
            {
              "4": {
                "backupId": 4,
                "timestamp": "2026-02-11T10:30:00Z",
                "status": "Waiting"
              }
            }
            """);

        var reader = new JsonStateReader(new TestPathProvider(statePath));

        var entries = reader.ReadEntries();

        Assert.True(entries.ContainsKey(4));
        Assert.Equal(4, entries[4].BackupId);
        Assert.Equal(State.BackupStatus.Waiting, entries[4].Status);
    }

    [Fact]
    public void ReadEntries_WhenJsonIsInvalid_ReturnsEmpty()
    {
        using var temp = new TempDirectory();
        var statePath = Path.Combine(temp.RootPath, "state.json");
        File.WriteAllText(statePath, "{ invalid");

        var reader = new JsonStateReader(new TestPathProvider(statePath));

        var entries = reader.ReadEntries();

        Assert.Empty(entries);
    }

    [Fact]
    public void ReadEntries_WhenFileIsEmpty_ReturnsEmpty()
    {
        using var temp = new TempDirectory();
        var statePath = Path.Combine(temp.RootPath, "state.json");
        File.WriteAllText(statePath, string.Empty);

        var reader = new JsonStateReader(new TestPathProvider(statePath));

        var entries = reader.ReadEntries();

        Assert.Empty(entries);
    }

    private sealed class TestPathProvider(string statePath) : IPathProvider
    {
        public string GetDailyLogPath(DateTime date, LogFormat format = LogFormat.Json)
            => Path.Combine(Path.GetDirectoryName(statePath)!, $"{date:yyyy-MM-dd}.json");

        public string GetStatePath() => statePath;

        public string GetJobsConfigPath() => Path.Combine(Path.GetDirectoryName(statePath)!, "jobs.json");

        public string GetUserPreferencesPath() => Path.Combine(Path.GetDirectoryName(statePath)!, "user-preferences.json");

        public void SetLogDirectoryOverride(string? directory)
        {
        }

        public string ResolveLogsDirectory() => Path.Combine(Path.GetDirectoryName(statePath)!, "logs");
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "easysave-json-state-reader-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootPath))
                {
                    Directory.Delete(RootPath, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
