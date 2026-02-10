using System.Reflection;

namespace EasySave.UI.Tests;

public class ProgramTests
{
    [Fact]
    public void InitServices_ResolvesExpectedCoreServices()
    {
        var initServices = typeof(Program).GetMethod("InitServices", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(initServices);

        var provider = (IServiceProvider)initServices!.Invoke(null, null)!;
        try
        {
            Assert.NotNull(provider.GetService(typeof(ConsoleUI)));
            Assert.NotNull(provider.GetService(typeof(BackupApplicationService)));
            Assert.NotNull(provider.GetService(typeof(IBackupEngine)));
            Assert.NotNull(provider.GetService(typeof(IBackupJobRepository)));
            Assert.NotNull(provider.GetService(typeof(IUserPreferencesRepository)));
            Assert.NotNull(provider.GetService(typeof(IStateWriter)));
        }
        finally
        {
            if (provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    [Fact]
    public void CreateLogger_WhenPathProviderThrows_ReturnsNoOpLogger()
    {
        var createLogger = typeof(Program).GetMethod("CreateLogger", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(createLogger);

        var logger = (ILogger)createLogger!.Invoke(null, [new ThrowingPathProvider()])!;

        Assert.IsType<NoOpLogger>(logger);
    }

    [Fact]
    public void CreateLogger_WithXmlPreferences_UsesXmlFormatOnWrite()
    {
        var createLogger = typeof(Program).GetMethod("CreateLogger", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(createLogger);

        var pathProvider = new RecordingPathProvider(LogFormat.Xml);
        var logger = (ILogger)createLogger!.Invoke(null, [pathProvider])!;

        logger.Write(new LogEntry(
            DateTime.UtcNow,
            "job",
            LogEventType.TransferFile,
            "src",
            "dst",
            12,
            34));

        Assert.Equal(LogFormat.Xml, pathProvider.LastRequestedFormat);
    }

    private sealed class ThrowingPathProvider : IPathProvider
    {
        public string GetDailyLogPath(DateTime date, LogFormat format = LogFormat.Json) => throw new InvalidOperationException("boom");

        public string GetStatePath() => throw new InvalidOperationException("boom");

        public string GetJobsConfigPath() => throw new InvalidOperationException("boom");

        public string GetUserPreferencesPath() => throw new InvalidOperationException("boom");

        public void SetLogDirectoryOverride(string? directory) => throw new InvalidOperationException("boom");
    }

    private sealed class RecordingPathProvider : IPathProvider
    {
        private readonly string _rootDirectory;
        private readonly string _preferencesPath;

        public LogFormat LastRequestedFormat { get; private set; } = LogFormat.Json;

        public RecordingPathProvider(LogFormat storedPreference)
        {
            _rootDirectory = Path.Combine(Path.GetTempPath(), "easysave-ui-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_rootDirectory);

            _preferencesPath = Path.Combine(_rootDirectory, "user-preferences.json");
            File.WriteAllText(
                _preferencesPath,
                $$"""
                {
                  "language": "fr",
                  "logDirectory": null,
                  "logFormat": "{{storedPreference.ToString().ToLowerInvariant()}}"
                }
                """);
        }

        public string GetDailyLogPath(DateTime date, LogFormat format = LogFormat.Json)
        {
            LastRequestedFormat = format;
            var extension = format == LogFormat.Xml ? "xml" : "json";
            var path = Path.Combine(_rootDirectory, $"{date:yyyy-MM-dd}.{extension}");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }

        public string GetStatePath() => Path.Combine(_rootDirectory, "state.json");

        public string GetJobsConfigPath() => Path.Combine(_rootDirectory, "jobs.json");

        public string GetUserPreferencesPath() => _preferencesPath;

        public void SetLogDirectoryOverride(string? directory)
        {
        }
    }
}
