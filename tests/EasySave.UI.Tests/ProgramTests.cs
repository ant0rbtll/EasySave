using System.Reflection;
using EasySave.AppCommon;
using EasySave.AppCommon.Services;
using EasySave.Application.Readers;
using EasySave.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EasySave.UI.Tests;

public class ProgramTests
{
    [Fact]
    public void Host_Run_WithNoArgs_CallsConsoleMainMenu()
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

        var services = new ServiceCollection();
        services.AddSingleton(ui);
        var serviceProvider = services.BuildServiceProvider();

        var host = new Host();
        host.Run(serviceProvider, []);

        Assert.Single(menuService.ShownMenuConfigs);
        Assert.Equal(LocalizationKey.menu, menuService.ShownMenuConfigs[0].Label);
    }

    [Fact]
    public void Host_Run_WithArgs_CallsConsoleRunFromArgs()
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

        var services = new ServiceCollection();
        services.AddSingleton(ui);
        var serviceProvider = services.BuildServiceProvider();

        var host = new Host();
        host.Run(serviceProvider, ["1"]);

        Assert.Single(engine.ExecutedJobs);
        Assert.Equal(1, menuService.WaitCalls);
    }

    [Fact]
    public void ApplicationManager_ResolvesExpectedCoreServices()
    {
        var manager = new ApplicationManager([]);

        // Use the Host to add UI-specific services, then verify core services resolve
        var servicesField = typeof(ApplicationManager).GetField("_services", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(servicesField);

        var services = (IServiceCollection)servicesField!.GetValue(manager)!;
        var host = new Host();
        host.ConfigureServices(services, []);

        var provider = services.BuildServiceProvider();
        try
        {
            Assert.NotNull(provider.GetService(typeof(ConsoleUI)));
            Assert.NotNull(provider.GetService(typeof(BackupApplicationService)));
            Assert.NotNull(provider.GetService(typeof(IBackupEngine)));
            Assert.NotNull(provider.GetService(typeof(IBackupJobRepository)));
            Assert.NotNull(provider.GetService(typeof(IUserPreferencesRepository)));
            Assert.NotNull(provider.GetService(typeof(IStateWriter)));
            Assert.NotNull(provider.GetService(typeof(IStateReader)));
            Assert.NotNull(provider.GetService(typeof(IBackupJobStateService)));
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
    public void ReloadableLogger_WhenPathProviderThrows_UsesNoOpFallback()
    {
        var logger = new ReloadableLogger(
            new FakeUserPreferencesRepository { Preferences = new UserPreferences { LogFormat = LogFormat.Json } },
            new ThrowingPathProvider(),
            new LogServerStatusNotifier());

        logger.Write(new LogEntry(DateTime.UtcNow, "job", LogEventType.TransferFile, "src", "dst", 12, 34));

        Assert.IsType<NoOpLogger>(GetCurrentLogger(logger));
    }

    [Fact]
    public void ReloadableLogger_WithXmlPreferences_UsesXmlFormatOnWrite()
    {
        var pathProvider = new RecordingPathProvider();
        using var logger = new ReloadableLogger(
            new FakeUserPreferencesRepository { Preferences = new UserPreferences { LogFormat = LogFormat.Xml } },
            pathProvider,
            new LogServerStatusNotifier());

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
    public void ReloadableLogger_WithJsonPreferences_UsesJsonFormatOnWrite()
    {
        var pathProvider = new RecordingPathProvider();
        using var logger = new ReloadableLogger(
            new FakeUserPreferencesRepository { Preferences = new UserPreferences { LogFormat = LogFormat.Json } },
            pathProvider,
            new LogServerStatusNotifier());

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

    private static ILogger GetCurrentLogger(ReloadableLogger logger)
    {
        var field = typeof(ReloadableLogger).GetField("_currentLogger", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<ILogger>(field!.GetValue(logger));
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
        private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "easysave-ui-tests", Guid.NewGuid().ToString("N"));

        public LogFormat LastRequestedFormat { get; private set; } = LogFormat.Json;

        public RecordingPathProvider()
        {
            Directory.CreateDirectory(_rootDirectory);
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

        public string GetUserPreferencesPath() => Path.Combine(_rootDirectory, "prefs.json");

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
        return new BackupApplicationService(repository, engine, Moq.Mock.Of<IBackupJobStateService>());
    }
}
