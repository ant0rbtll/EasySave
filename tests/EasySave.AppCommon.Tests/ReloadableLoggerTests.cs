using System.Reflection;
using EasySave.Configuration;
using EasySave.Persistence;

namespace EasySave.AppCommon.Tests;

public class ReloadableLoggerTests : IDisposable
{
    private readonly string _tempDir;

    public ReloadableLoggerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "EasySave-AppCommon-Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch { /* best-effort cleanup */ }
        GC.SuppressFinalize(this);
    }

    private static ILogger GetCurrentLogger(ReloadableLogger logger)
    {
        var field = typeof(ReloadableLogger).GetField("_currentLogger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<ILogger>(field!.GetValue(logger));
    }

    private IPathProvider CreatePathProvider()
        => new TestPathProvider(_tempDir);

    private static IUserPreferencesRepository CreatePreferences(
        LogMode logMode, string? serverUrl = null, LogFormat logFormat = LogFormat.Json)
    {
        return new FakePreferencesRepository(new UserPreferences
        {
            LogFormat = logFormat,
            LogMode = logMode,
            LogServerUrl = serverUrl
        });
    }

    [Fact]
    public void CentralizedMode_WithServerUrl_CreatesResilientHttpLogSender()
    {
        var notifier = new LogServerStatusNotifier();
        using var logger = new ReloadableLogger(
            CreatePreferences(LogMode.Centralized, "http://localhost:1"),
            CreatePathProvider(),
            notifier);

        var inner = GetCurrentLogger(logger);
        Assert.IsType<ResilientHttpLogSender>(inner);
    }

    [Fact]
    public void CentralizedMode_WithoutServerUrl_CreatesNoOpLogger()
    {
        var notifier = new LogServerStatusNotifier();
        // Force to false first so we can verify it gets reset to true
        notifier.ReportStatus(false);

        using var logger = new ReloadableLogger(
            CreatePreferences(LogMode.Centralized, serverUrl: null),
            CreatePathProvider(),
            notifier);

        var inner = GetCurrentLogger(logger);
        Assert.IsType<NoOpLogger>(inner);
        Assert.True(notifier.IsServerReachable);
    }

    [Fact]
    public void LocalAndCentralizedMode_WithServerUrl_CreatesCompositeLogger()
    {
        var notifier = new LogServerStatusNotifier();
        using var logger = new ReloadableLogger(
            CreatePreferences(LogMode.LocalAndCentralized, "http://localhost:1"),
            CreatePathProvider(),
            notifier);

        var inner = GetCurrentLogger(logger);
        Assert.IsType<CompositeLogger>(inner);
    }

    [Fact]
    public void LocalAndCentralizedMode_WithoutServerUrl_CreatesLocalLoggerOnly()
    {
        var notifier = new LogServerStatusNotifier();
        using var logger = new ReloadableLogger(
            CreatePreferences(LogMode.LocalAndCentralized, serverUrl: null),
            CreatePathProvider(),
            notifier);

        var inner = GetCurrentLogger(logger);
        // Without a server URL, CompositeLogger is not created — just the local logger
        Assert.IsNotType<CompositeLogger>(inner);
        Assert.IsNotType<NoOpLogger>(inner);
    }

    [Fact]
    public void Reload_SwitchMode_ReplacesLogger()
    {
        var notifier = new LogServerStatusNotifier();
        var prefs = new FakePreferencesRepository(new UserPreferences
        {
            LogFormat = LogFormat.Json,
            LogMode = LogMode.Local
        });
        using var logger = new ReloadableLogger(prefs, CreatePathProvider(), notifier);

        var before = GetCurrentLogger(logger);
        Assert.IsNotType<ResilientHttpLogSender>(before);

        // Switch to Centralized
        prefs.Preferences = new UserPreferences
        {
            LogFormat = LogFormat.Json,
            LogMode = LogMode.Centralized,
            LogServerUrl = "http://localhost:1"
        };
        logger.Reload();

        var after = GetCurrentLogger(logger);
        Assert.IsType<ResilientHttpLogSender>(after);
    }

    // ── Test doubles ──────────────────────────────────────────────

    private sealed class FakePreferencesRepository : IUserPreferencesRepository
    {
        public UserPreferences Preferences { get; set; }

        public FakePreferencesRepository(UserPreferences preferences)
        {
            Preferences = preferences;
        }

        public UserPreferences Load() => Preferences;
        public void Save(UserPreferences preferences) { Preferences = preferences; }
    }

    private sealed class TestPathProvider : IPathProvider
    {
        private readonly string _rootDir;
        private string? _logDirOverride;

        public TestPathProvider(string rootDir) => _rootDir = rootDir;

        public string GetDailyLogPath(DateTime date, LogFormat format = LogFormat.Json)
        {
            var dir = _logDirOverride ?? _rootDir;
            var ext = format == LogFormat.Xml ? "xml" : "json";
            return Path.Combine(dir, $"{date:yyyy-MM-dd}.{ext}");
        }

        public string GetStatePath() => Path.Combine(_rootDir, "state.json");
        public string GetJobsConfigPath() => Path.Combine(_rootDir, "jobs.json");
        public string GetUserPreferencesPath() => Path.Combine(_rootDir, "prefs.json");
        public void SetLogDirectoryOverride(string? directory) => _logDirOverride = directory;
        public string ResolveLogsDirectory() => _logDirOverride ?? _rootDir;
    }
}
