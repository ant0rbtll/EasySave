namespace EasySave.UI.Tests;

public class ConsoleUITests
{
    [Fact]
    public void MainMenu_UsesCurrentJobCountAndShowsConfig()
    {
        var app = CreateApplicationService(out _);
        app.CreateJob("A", "S", "D", BackupType.Complete);

        var console = new FakeConsoleAdapter();
        var menuService = new FakeMenuService();
        var messageService = new FakeConsoleMessageService();
        var inputService = new FakeConsoleInputService();
        var localization = new FakeLocalizationService();
        var prefsRepo = new FakeUserPreferencesRepository { Preferences = new UserPreferences() };
        var flowsFactory = new MenuFactory();
        var uiFactory = new CapturingMainMenuFactory();
        var jobsFlow = new JobsFlowService(app, menuService, flowsFactory, messageService, inputService, console, new JobEditSessionService());
        var settingsFlow = new SettingsFlowService(
            prefsRepo,
            prefsRepo.Preferences,
            new FakePathProvider(),
            localization,
            menuService,
            flowsFactory,
            messageService,
            inputService,
            console);

        var ui = new ConsoleUI(
            app,
            new CommandLineParser(),
            localization,
            console,
            menuService,
            uiFactory,
            messageService,
            inputService,
            jobsFlow,
            settingsFlow);

        ui.MainMenu();

        Assert.Equal(1, uiFactory.ReceivedCurrentJobCount);
        Assert.Single(menuService.ShownMenuConfigs);
        uiFactory.LastOnQuit!.Invoke();
        Assert.Contains("CLEAR", console.Events);
    }

    [Fact]
    public void RunFromArgs_WithValidArgs_RunsJobsAndWaits()
    {
        var app = CreateApplicationService(out var engine);
        app.CreateJob("A", "S", "D", BackupType.Complete);

        var ui = CreateConsoleUiForRun(app, out var menuService, out var messageService);

        ui.RunFromArgs(["1"]);

        Assert.Single(engine.ExecutedJobs);
        Assert.Equal(1, menuService.WaitCalls);
        Assert.Empty(messageService.Errors);
    }

    [Fact]
    public void RunFromArgs_WithInvalidArgs_ShowsErrorAndWaits()
    {
        var app = CreateApplicationService(out _);
        var ui = CreateConsoleUiForRun(app, out var menuService, out var messageService);

        ui.RunFromArgs(["1--2"]);

        Assert.Single(messageService.Errors);
        Assert.Equal(1, menuService.WaitCalls);
    }

    [Fact]
    public void PublicConstructor_InitializesLocalizationFromPreferences()
    {
        var app = CreateApplicationService(out _);
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "en", LogFormat = LogFormat.Json }
        };

        var ui = new ConsoleUI(app, repo, new FakePathProvider(), new CommandLineParser());

        Assert.Equal("en", ui.LocalizationService.Culture);
    }

    [Fact]
    public void PublicConstructor_WithInvalidLanguage_FallsBackToFrenchAndPersists()
    {
        var app = CreateApplicationService(out _);
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "zz", LogFormat = LogFormat.Json }
        };

        var ui = new ConsoleUI(app, repo, new FakePathProvider(), new CommandLineParser());

        Assert.Equal("fr", ui.LocalizationService.Culture);
        Assert.Equal(1, repo.SaveCalls);
    }

    [Fact]
    public void RunFromArgs_WhenRunJobThrows_ShowsErrorAndWaits()
    {
        var repository = new InMemoryBackupJobRepository(new SequentialJobIdProvider());
        var app = new BackupApplicationService(repository, new ThrowingBackupEngine());
        app.CreateJob("A", "S", "D", BackupType.Complete);

        var ui = CreateConsoleUiForRun(app, out var menuService, out var messageService);

        ui.RunFromArgs(["1"]);

        Assert.Single(messageService.Errors);
        Assert.Equal(1, menuService.WaitCalls);
    }

    private static ConsoleUI CreateConsoleUiForRun(
        BackupApplicationService app,
        out FakeMenuService menuService,
        out FakeConsoleMessageService messageService)
    {
        var console = new FakeConsoleAdapter();
        var inputService = new FakeConsoleInputService();
        messageService = new FakeConsoleMessageService();
        menuService = new FakeMenuService();
        var localization = new FakeLocalizationService();
        var prefsRepo = new FakeUserPreferencesRepository { Preferences = new UserPreferences() };
        var flowFactory = new MenuFactory();

        var jobsFlow = new JobsFlowService(app, menuService, flowFactory, messageService, inputService, console, new JobEditSessionService());
        var settingsFlow = new SettingsFlowService(
            prefsRepo,
            prefsRepo.Preferences,
            new FakePathProvider(),
            localization,
            menuService,
            flowFactory,
            messageService,
            inputService,
            console);

        return new ConsoleUI(
            app,
            new CommandLineParser(),
            localization,
            console,
            menuService,
            flowFactory,
            messageService,
            inputService,
            jobsFlow,
            settingsFlow);
    }

    private static BackupApplicationService CreateApplicationService(out FakeBackupEngine engine)
    {
        var repository = new InMemoryBackupJobRepository(new SequentialJobIdProvider());
        engine = new FakeBackupEngine();
        return new BackupApplicationService(repository, engine);
    }

    private sealed class CapturingMainMenuFactory : IMenuFactory
    {
        public int ReceivedCurrentJobCount { get; private set; } = -1;
        public Action? LastOnQuit { get; private set; }

        public MenuConfig CreateMainMenu(int currentJobCount, Action onCreateJob, Action onManageJobs, Action onConfigureParams, Action onQuit)
        {
            ReceivedCurrentJobCount = currentJobCount;
            LastOnQuit = onQuit;
            return new MenuConfig(
                [LocalizationKey.menu_quit],
                new Dictionary<int, Action> { [0] = onQuit },
                LocalizationKey.menu);
        }

        public MenuConfig CreateLocaleMenu(IReadOnlyDictionary<string, LocalizationKey> cultures, Action<string> onSelectLocale, Action onBack, Action? renderHeader = null)
            => throw new NotSupportedException();

        public MenuConfig CreateParamsMenu(Action onShowChangeLocale, Action onShowChangeLogDirectory, Action onShowChangeLogFormat, Action onBack, Action? renderHeader = null)
            => throw new NotSupportedException();

        public MenuConfig CreateLogFormatMenu(string jsonLabel, string xmlLabel, string backLabel, Action onJson, Action onXml, Action onBack, Action? renderHeader = null)
            => throw new NotSupportedException();

        public MenuConfig CreateJobsListMenu(IEnumerable<BackupJob> jobs, string backLabel, Action<BackupJob> onSelectJob, Action onBack)
            => throw new NotSupportedException();

        public MenuConfig CreateJobDetailsMenu(BackupJob job, Action<BackupJob> onRunJob, Action<BackupJob> onUpdateJob, Action<BackupJob> onDeleteJob, Action onBack, Action? renderHeader = null)
            => throw new NotSupportedException();

        public MenuConfig CreateJobUpdateMenu(BackupJob job, Action<BackupJob, JobEditableField> onUpdateField, Action<BackupJob> onSave, Action<BackupJob> onBack)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingBackupEngine : IBackupEngine
    {
        public void Execute(BackupJob job)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
