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

    public ILocalizationService LocalizationService { get; }

    public ConsoleUI(
        BackupApplicationService backupApplicationService,
        IUserPreferencesRepository preferencesRepository,
        IPathProvider pathProvider,
        CommandLineParser parser)
    {
        _parser = parser;
        _backupApplicationService = backupApplicationService;

        LocalizationService = new LocalizationService();
        _menuService = new MenuService(LocalizationService);
        _menuFactory = new MenuFactory();
        _consoleAdapter = new SystemConsoleAdapter();

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
            _inputService);

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
