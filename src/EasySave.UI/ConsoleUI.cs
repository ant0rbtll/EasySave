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

    /// <summary>
    /// Initializes a test-friendly console UI using prebuilt collaborators.
    /// </summary>
    private void DisplayJobsList()
    {
        try
        {
            List<BackupJob> backupJobList = _backupApplicationService.GetAllJobs();
            foreach (BackupJob job in backupJobList)
            {
                _consoleAdapter.WriteLine(job.Id + " - " + job.Name);
                string lastExecution = job.LastExecutionDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "never";
                _consoleAdapter.WriteLine($"    LastExecution: {lastExecution} | Active: {job.IsActive}");
            }
        }
        catch (Exception e)
        {
            ShowError(e);
            _menuService.WaitForUser();
            MainMenu();
        }
    }

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
    /// Shows interactive list of backup jobs
    /// </summary>
    public void ShowJobsList()
    {
        var menuConfig = _menuFactory.CreateJobsListMenu();
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Shows details of a specific backup job with action options
    /// </summary>
    public void ShowJobDetails(BackupJob job)
    {
        var refreshedJob = _backupApplicationService.GetJobById(job.Id) ?? job;
        Action renderJobDetails = () =>
        {
            ShowMessage(LocalizationKey.backupjob_id, false);
            _consoleAdapter.WriteLine($": {refreshedJob.Id}");

            ShowMessage(LocalizationKey.backupjob_name, false);
            _consoleAdapter.WriteLine($": {refreshedJob.Name}");

            ShowMessage(LocalizationKey.backupjob_source, false);
            _consoleAdapter.WriteLine($": {refreshedJob.Source}");

            ShowMessage(LocalizationKey.backupjob_destination, false);
            _consoleAdapter.WriteLine($": {refreshedJob.Destination}");

            ShowMessage(LocalizationKey.backupjob_type, false);
            _consoleAdapter.WriteLine($": {refreshedJob.Type}");

            _consoleAdapter.WriteLine($": LastExecution {(refreshedJob.LastExecutionDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "never")}");
            _consoleAdapter.WriteLine($": Active {refreshedJob.IsActive}");

            _consoleAdapter.WriteLine();
        };

        var menuConfig = _menuFactory.CreateJobDetailsMenu(refreshedJob, renderJobDetails);
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Runs a backup job
    /// </summary>
    public void RunJob(BackupJob job)
    {
        _consoleAdapter.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_job_run);

        ShowMessage(LocalizationKey.backupjob_running);
        try
        {
            _backupApplicationService.RunJobById(job.Id);
            ShowMessage(LocalizationKey.backupjob_completed);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }

        _menuService.WaitForUser();
        ShowJobsList();
    }

    /// <summary>
    /// Updates a backup job
    /// </summary>
    public void UpdateJob(BackupJob job)
    {
        _consoleAdapter.Clear();
        var menuConfig = _menuFactory.CreateJobUpdateMenu(job);
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Updates a specific field of a backup job
    /// </summary>
    public void UpdateJobField(BackupJob job, string field)
    {
        _consoleAdapter.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_job_update);

        switch (field)
        {
            case "name":
                string? newName = AskString(LocalizationKey.menu_job_update_name);
                if (newName != null) job.Name = newName;
                break;
            case "source":
                string? newSource = AskString(LocalizationKey.menu_job_update_source);
                if (newSource != null) job.Source = newSource;
                break;
            case "destination":
                string? newDestination = AskString(LocalizationKey.menu_job_update_destination);
                if (newDestination != null) job.Destination = newDestination;
                break;
            case "type":
                BackupType? newType = AskBackupType(LocalizationKey.menu_job_update_type);
                if (newType != null) job.Type = newType.Value;
                break;
        }

        UpdateJob(job);
    }

    /// <summary>
    /// Saves the updated backup job
    /// </summary>
    public void SaveJobUpdate(BackupJob job)
    {
        try
        {
            _backupApplicationService.UpdateJob(job);
            ShowMessage(LocalizationKey.backupjob_updated);
        }
        catch (Exception e)
        {
            ShowError(e);
        }
        _menuService.WaitForUser();
        ShowJobsList();
    }

    /// <summary>
    /// Deletes a backup job with confirmation
    /// </summary>
    public void DeleteJob(BackupJob job)
    {
        _consoleAdapter.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_job_delete);

        ShowMessage(LocalizationKey.backupjob_name, false);
        _consoleAdapter.WriteLine($": {job.Name}");
        _consoleAdapter.WriteLine();

        ShowMessage(LocalizationKey.backupjob_delete_confirm);
        var key = _consoleAdapter.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Y || key.Key == ConsoleKey.Enter)
        {
            try
            {
                _backupApplicationService.RemoveJob(job.Id);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            ShowMessage(LocalizationKey.backupjob_deleted);
        }
        else
        {
            ShowJobDetails(job);
        }
        _menuService.WaitForUser();
        ShowJobsList();
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
