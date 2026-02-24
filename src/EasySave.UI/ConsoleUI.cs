using EasySave.Application.Services;
using EasySave.Core;
using EasySave.Localization;
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
    /// Initializes a test-friendly console UI using prebuilt collaborators.
    /// </summary>
    /// <param name="backupApplicationService">Application service exposing backup use cases.</param>
    /// <param name="parser">Command-line parser for non-interactive execution.</param>
    /// <param name="localizationService">Localization service used by the UI.</param>
    /// <param name="consoleAdapter">Console adapter abstraction.</param>
    /// <param name="menuService">Menu rendering service.</param>
    /// <param name="menuFactory">Menu config factory.</param>
    /// <param name="messageService">Console message service.</param>
    /// <param name="inputService">Console input service.</param>
    /// <param name="jobsFlowService">Jobs workflow service.</param>
    /// <param name="settingsFlowService">Settings workflow service.</param>
    internal ConsoleUI(
        BackupApplicationService backupApplicationService,
        CommandLineParser parser,
        ILocalizationService localizationService,
        IConsoleAdapter consoleAdapter,
        IMenuService menuService,
        IMenuFactory menuFactory,
        IConsoleMessageService messageService,
        IConsoleInputService inputService,
        JobsFlowService jobsFlowService,
        SettingsFlowService settingsFlowService)
    {
        _parser = parser;
        _backupApplicationService = backupApplicationService;
        LocalizationService = localizationService;
        _consoleAdapter = consoleAdapter;
        _menuService = menuService;
        _menuFactory = menuFactory;
        _messageService = messageService;
        _inputService = inputService;
        _jobsFlowService = jobsFlowService;
        _settingsFlowService = settingsFlowService;
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
    /// Shows interactive list of backup jobs.
    /// </summary>
    public void ShowJobsList()
    {
        _jobsFlowService.ShowJobsList(MainMenu);
    }

    /// <summary>
    /// Shows details of a specific backup job with action options.
    /// </summary>
    public void ShowJobDetails(BackupJob job)
    {
        var refreshedJob = _backupApplicationService.GetJob(job.Id) ?? job;

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

            ShowMessage(LocalizationKey.backupjob_last_executed, false);
            _consoleAdapter.Write(": ");
            if (refreshedJob.LastExecutionDate is not null)
            {
                _consoleAdapter.WriteLine(refreshedJob.LastExecutionDate.Value.ToString("g"));
            }
            else
            {
                ShowMessage(LocalizationKey.backupjob_never);
            }

            ShowMessage(LocalizationKey.backupjob_active, false);
            _consoleAdapter.WriteLine($": {refreshedJob.IsActive}");
            _consoleAdapter.WriteLine();
        };

        var menuConfig = _menuFactory.CreateJobDetailsMenu(
            refreshedJob,
            backupJob => RunJob(backupJob),
            backupJob => UpdateJob(backupJob),
            backupJob => DeleteJob(backupJob),
            ShowJobsList,
            renderJobDetails);
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Runs a backup job.
    /// </summary>
    public void RunJob(BackupJob job)
    {
        _consoleAdapter.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_job_run);

        ShowMessage(LocalizationKey.backupjob_running);
        try
        {
            _backupApplicationService.RunJob(job.Id).GetAwaiter().GetResult();
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
    /// Updates a backup job.
    /// </summary>
    public void UpdateJob(BackupJob job)
    {
        _consoleAdapter.Clear();
        var menuConfig = _menuFactory.CreateJobUpdateMenu(
            job,
            (backupJob, field) => UpdateJobField(backupJob, field),
            backupJob => SaveJobUpdate(backupJob),
            backupJob => ShowJobDetails(backupJob));
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Updates a specific field of a backup job.
    /// </summary>
    private void UpdateJobField(BackupJob job, JobEditableField field)
    {
        _consoleAdapter.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_job_update);

        switch (field)
        {
            case JobEditableField.Name:
                string? newName = AskString(LocalizationKey.menu_job_update_name);
                if (newName != null) job.Name = newName;
                break;
            case JobEditableField.Source:
                string? newSource = AskString(LocalizationKey.menu_job_update_source);
                if (newSource != null) job.Source = newSource;
                break;
            case JobEditableField.Destination:
                string? newDestination = AskString(LocalizationKey.menu_job_update_destination);
                if (newDestination != null) job.Destination = newDestination;
                break;
            case JobEditableField.Type:
                BackupType? newType = AskBackupType(LocalizationKey.menu_job_update_type);
                if (newType != null) job.Type = newType.Value;
                break;
        }

        UpdateJob(job);
    }

    /// <summary>
    /// Saves the updated backup job.
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
    /// Deletes a backup job with confirmation.
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
            _backupApplicationService.RunJobs(jobs).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            _messageService.ShowError(exception);
        }

        _menuService.WaitForUser();
    }

    private string? AskString(LocalizationKey key)
    {
        return _inputService.AskString(key);
    }

    private BackupType? AskBackupType(LocalizationKey key)
    {
        return _inputService.AskBackupType(key);
    }

    private void ShowMessage(LocalizationKey key, bool newLine = true)
    {
        _messageService.Write(key, newLine);
    }

    private void ShowError(Exception exception)
    {
        _messageService.ShowError(exception);
    }
}
