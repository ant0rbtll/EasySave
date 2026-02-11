using System.Reflection;

namespace EasySave.UI.Tests;

public class ProgramTests
{
    [Fact]
    public void Main_WithNoArgs_CallsConsoleMainMenu()
    {
        var app = CreateApplicationService(out _);
        var console = new FakeConsoleAdapter();
        var menuService = new FakeMenuService();
        var inputService = new FakeConsoleInputService();
        var messageService = new FakeConsoleMessageService();
        var localization = new FakeLocalizationService();
        var prefsRepo = new FakeUserPreferencesRepository { Preferences = new UserPreferences() };
        var menuFactory = new MenuFactory();
        var jobsFlow = new JobsFlowService(app, menuService, menuFactory, messageService, inputService, console, new JobEditSessionService());
        var settingsFlow = new SettingsFlowService(
            prefsRepo,
            prefsRepo.Preferences,
            new FakePathProvider(),
            localization,
            menuService,
            menuFactory,
            messageService,
            inputService,
            console);

        var ui = new ConsoleUI(
            app,
            new CommandLineParser(),
            localization,
            console,
            menuService,
            menuFactory,
            messageService,
            inputService,
            jobsFlow,
            settingsFlow);

        var previousFactory = Program.ServiceProviderFactory;
        Program.ServiceProviderFactory = () => new FakeServiceProvider(ui);
        try
        {
            Program.Main([]);
        }
        finally
        {
            Program.ServiceProviderFactory = previousFactory;
        }

        Assert.Single(menuService.ShownMenuConfigs);
        Assert.Equal(LocalizationKey.menu, menuService.ShownMenuConfigs[0].Label);
    }

    [Fact]
    public void Main_WithArgs_CallsConsoleRunFromArgs()
    {
        var app = CreateApplicationService(out var engine);
        app.CreateJob("A", "S", "D", BackupType.Complete);

        var console = new FakeConsoleAdapter();
        var menuService = new FakeMenuService();
        var inputService = new FakeConsoleInputService();
        var messageService = new FakeConsoleMessageService();
        var localization = new FakeLocalizationService();
        var prefsRepo = new FakeUserPreferencesRepository { Preferences = new UserPreferences() };
        var menuFactory = new MenuFactory();
        var jobsFlow = new JobsFlowService(app, menuService, menuFactory, messageService, inputService, console, new JobEditSessionService());
        var settingsFlow = new SettingsFlowService(
            prefsRepo,
            prefsRepo.Preferences,
            new FakePathProvider(),
            localization,
            menuService,
            menuFactory,
            messageService,
            inputService,
            console);

        var ui = new ConsoleUI(
            app,
            new CommandLineParser(),
            localization,
            console,
            menuService,
            menuFactory,
            messageService,
            inputService,
            jobsFlow,
            settingsFlow);

        var previousFactory = Program.ServiceProviderFactory;
        Program.ServiceProviderFactory = () => new FakeServiceProvider(ui);
        try
        {
            Program.Main(["1"]);
        }
        finally
        {
            Program.ServiceProviderFactory = previousFactory;
        }

        Assert.Single(engine.ExecutedJobs);
        Assert.Equal(1, menuService.WaitCalls);
    }

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

    [Fact]
    public void CreateLogger_WithJsonPreferences_UsesJsonFormatOnWrite()
    {
        var createLogger = typeof(Program).GetMethod("CreateLogger", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(createLogger);

        var pathProvider = new RecordingPathProvider(LogFormat.Json);
        var logger = (ILogger)createLogger!.Invoke(null, [pathProvider])!;

        logger.Write(new LogEntry(
            DateTime.UtcNow,
            "job",
            LogEventType.TransferFile,
            "src",
            "dst",
            12,
            34));

        Assert.Equal(LogFormat.Json, pathProvider.LastRequestedFormat);
    }

    private sealed class ThrowingPathProvider : IPathProvider
    {
        public string GetDailyLogPath(DateTime date, LogFormat format = LogFormat.Json) => throw new InvalidOperationException("boom");

        public string GetStatePath() => throw new InvalidOperationException("boom");

        public string GetJobsConfigPath() => throw new InvalidOperationException("boom");

        public string GetUserPreferencesPath() => throw new InvalidOperationException("boom");

        public void SetLogDirectoryOverride(string? directory) => throw new InvalidOperationException("boom");

        public string ResolveLogsDirectory() => throw new InvalidOperationException("boom");
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

        public string ResolveLogsDirectory()
            => _rootDirectory;
    }

    private static BackupApplicationService CreateApplicationService(out FakeBackupEngine engine)
    {
        var repository = new InMemoryBackupJobRepository(new SequentialJobIdProvider());
        engine = new FakeBackupEngine();
        return new BackupApplicationService(repository, engine);
    }

    private sealed class FakeServiceProvider(ConsoleUI ui) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ConsoleUI))
            {
                return ui;
            }

            return null;
        }
    }
}
