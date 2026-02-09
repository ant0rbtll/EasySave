using EasySave.Application;
using EasySave.Configuration;
using EasySave.Localization;
using EasySave.Persistence;
using EasySave.UI.Menu;
using EasySave.UI.Services;

namespace EasySave.UI;

/// <summary>
/// Console-based user interface for the EasySave application.
/// </summary>
public class ConsoleUI
{
    private readonly CommandLineParser _parser;
    private readonly BackupApplicationService _backupApplicationService;
    private readonly IMenuService _menuService;
    private readonly IMenuFactory _menuFactory;
    private readonly IConsoleAdapter _consoleAdapter;
    private readonly IConsoleMessageService _messageService;
    private readonly IConsoleInputService _inputService;
    private readonly JobsFlowService _jobsFlowService;
    private readonly SettingsFlowService _settingsFlowService;

    /// <summary>
    /// Gets the localization service used by the console UI.
    /// </summary>
    public ILocalizationService LocalizationService { get; }

    /// <summary>
    /// Initializes the console UI and wires all flow services.
    /// </summary>
    /// <param name="backupApplicationService">Application service exposing backup use cases.</param>
    /// <param name="preferencesRepository">Repository used to load and persist user preferences.</param>
    /// <param name="pathProvider">Path provider used by settings flows.</param>
    /// <param name="parser">Command-line parser for non-interactive execution.</param>
    public ConsoleUI(
        BackupApplicationService backupApplicationService,
        IUserPreferencesRepository preferencesRepository,
        IPathProvider pathProvider,
        CommandLineParser parser)
    {
        _parser = parser;
        _backupApplicationService = backupApplicationService;

        LocalizationService = new LocalizationService();
        _consoleAdapter = new SystemConsoleAdapter();
        _menuService = new MenuService(LocalizationService, _consoleAdapter);
        _menuFactory = new MenuFactory();

        _messageService = new ConsoleMessageService(LocalizationService, new ErrorManager(), _consoleAdapter);
        _inputService = new ConsoleInputService(_messageService, _consoleAdapter);

        var userPreferences = preferencesRepository.Load();
        _jobsFlowService = new JobsFlowService(
            _backupApplicationService,
            _menuService,
            _menuFactory,
            _messageService,
            _inputService,
            _consoleAdapter,
            new JobEditSessionService());

        _settingsFlowService = new SettingsFlowService(
            preferencesRepository,
            userPreferences,
            pathProvider,
            LocalizationService,
            _menuService,
            _menuFactory,
            _messageService,
            _inputService,
            _consoleAdapter);

        _settingsFlowService.InitializeCulture();
    }

    /// <summary>
    /// Displays the main menu of the application.
    /// </summary>
    public void MainMenu()
    {
        var menuConfig = _menuFactory.CreateMainMenu(
            _jobsFlowService.GetJobCount(),
            () => _jobsFlowService.CreateBackupJob(MainMenu),
            () => _jobsFlowService.ShowJobsList(MainMenu),
            () => _settingsFlowService.ConfigureParams(MainMenu),
            Quit);

        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    public void Quit()
    {
        _consoleAdapter.Clear();
    }

    /// <summary>
    /// Runs backup jobs from command-line arguments.
    /// </summary>
    /// <param name="args">Raw command-line arguments representing target jobs.</param>
    internal void RunFromArgs(string[] args)
    {
        try
        {
            var jobs = _parser.Parse(args);
            _backupApplicationService.RunJobs(jobs);
        }
        catch (Exception exception)
        {
            _messageService.ShowError(exception);
        }

        _menuService.WaitForUser();
    }
}
