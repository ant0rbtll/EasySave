using EasySave.Application;
using EasySave.Core;
using EasySave.Localization;
using EasySave.UI.Menu;

namespace EasySave.UI.Services;

/// <summary>
/// Handles backup jobs menu navigation and actions.
/// </summary>
internal class JobsFlowService(
    BackupApplicationService backupApplicationService,
    IMenuService menuService,
    IMenuFactory menuFactory,
    IConsoleMessageService messageService,
    IConsoleInputService inputService,
    IConsoleAdapter consoleAdapter,
    JobEditSessionService editSessionService)
{
    private readonly BackupApplicationService _backupApplicationService = backupApplicationService;
    private readonly IMenuService _menuService = menuService;
    private readonly IMenuFactory _menuFactory = menuFactory;
    private readonly IConsoleMessageService _messageService = messageService;
    private readonly IConsoleInputService _inputService = inputService;
    private readonly IConsoleAdapter _consoleAdapter = consoleAdapter;
    private readonly JobEditSessionService _editSessionService = editSessionService;

    /// <summary>
    /// Returns the number of configured backup jobs.
    /// </summary>
    /// <returns>Current backup jobs count.</returns>
    public int GetJobCount()
    {
        return _backupApplicationService.GetAllJobs().Count;
    }

    /// <summary>
    /// Runs the interactive workflow to create a new backup job.
    /// </summary>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    public void CreateBackupJob(Action onBackToMainMenu)
    {
        _menuService.DisplayLabel(LocalizationKey.menu_create);

        string? nameJob = _inputService.AskString(LocalizationKey.backupjob_create_name);
        if (nameJob == null) { onBackToMainMenu(); return; }

        string? sourceJob = _inputService.AskString(LocalizationKey.backupjob_create_source);
        if (sourceJob == null) { onBackToMainMenu(); return; }

        string? destinationJob = _inputService.AskString(LocalizationKey.backupjob_create_destination);
        if (destinationJob == null) { onBackToMainMenu(); return; }

        BackupType? backupTypeJob = _inputService.AskBackupType(LocalizationKey.backupjob_create_type);
        if (backupTypeJob == null) { onBackToMainMenu(); return; }

        try
        {
            _backupApplicationService.CreateJob(nameJob, sourceJob, destinationJob, backupTypeJob.Value);
            _messageService.WriteWithParams(LocalizationKey.backupjob_created_named, [nameJob]);
        }
        catch (Exception e)
        {
            _messageService.ShowError(e);
        }

        _menuService.WaitForUser();
        onBackToMainMenu();
    }

    /// <summary>
    /// Displays the jobs list menu.
    /// </summary>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    public void ShowJobsList(Action onBackToMainMenu)
    {
        var jobs = _backupApplicationService.GetAllJobs();
        var menuConfig = _menuFactory.CreateJobsListMenu(
            jobs,
            _messageService.Translate(LocalizationKey.back),
            job => ShowJobDetails(job, onBackToMainMenu),
            onBackToMainMenu);

        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Displays details and actions for a single backup job.
    /// </summary>
    /// <param name="job">Job to display.</param>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    private void ShowJobDetails(BackupJob job, Action onBackToMainMenu)
    {
        Action renderJobDetails = () =>
        {
            _messageService.Write(LocalizationKey.backupjob_id, false);
            _consoleAdapter.WriteLine($": {job.Id}");

            _messageService.Write(LocalizationKey.backupjob_name, false);
            _consoleAdapter.WriteLine($": {job.Name}");

            _messageService.Write(LocalizationKey.backupjob_source, false);
            _consoleAdapter.WriteLine($": {job.Source}");

            _messageService.Write(LocalizationKey.backupjob_destination, false);
            _consoleAdapter.WriteLine($": {job.Destination}");

            _messageService.Write(LocalizationKey.backupjob_type, false);
            _consoleAdapter.WriteLine($": {job.Type}");

            _consoleAdapter.WriteLine();
        };

        var menuConfig = _menuFactory.CreateJobDetailsMenu(
            job,
            backupJob => RunJob(backupJob, onBackToMainMenu),
            backupJob => UpdateJob(backupJob, onBackToMainMenu),
            backupJob => DeleteJob(backupJob, onBackToMainMenu),
            () => ShowJobsList(onBackToMainMenu),
            renderJobDetails);

        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Executes one backup job and returns to the jobs list.
    /// </summary>
    /// <param name="job">Job to execute.</param>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    private void RunJob(BackupJob job, Action onBackToMainMenu)
    {
        _consoleAdapter.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_job_run);

        _messageService.Write(LocalizationKey.backupjob_running);
        try
        {
            _backupApplicationService.RunJob(job.Id);
            _messageService.WriteWithParams(LocalizationKey.backupjob_completed_named, [job.Name]);
        }
        catch (Exception ex)
        {
            _messageService.ShowError(ex);
        }

        _menuService.WaitForUser();
        ShowJobsList(onBackToMainMenu);
    }

    /// <summary>
    /// Opens the job update menu for the selected job.
    /// </summary>
    /// <param name="job">Job to update.</param>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    private void UpdateJob(BackupJob job, Action onBackToMainMenu)
    {
        _consoleAdapter.Clear();
        _editSessionService.BeginOrRefresh(job);
        var menuConfig = _menuFactory.CreateJobUpdateMenu(
            job,
            (backupJob, field) => UpdateJobField(backupJob, field, onBackToMainMenu),
            backupJob => SaveJobUpdate(backupJob, onBackToMainMenu),
            backupJob => ExitJobUpdate(backupJob, onBackToMainMenu));

        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Updates one editable field of the selected job.
    /// </summary>
    /// <param name="job">Job being updated.</param>
    /// <param name="field">Field name to edit.</param>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    private void UpdateJobField(BackupJob job, string field, Action onBackToMainMenu)
    {
        _consoleAdapter.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_job_update);

        switch (field)
        {
            case "name":
                string? newName = _inputService.AskStringWithCurrentValue(LocalizationKey.menu_job_update_name, job.Name);
                if (newName != null) job.Name = newName;
                break;
            case "source":
                string? newSource = _inputService.AskStringWithCurrentValue(LocalizationKey.menu_job_update_source, job.Source);
                if (newSource != null) job.Source = newSource;
                break;
            case "destination":
                string? newDestination = _inputService.AskStringWithCurrentValue(LocalizationKey.menu_job_update_destination, job.Destination);
                if (newDestination != null) job.Destination = newDestination;
                break;
            case "type":
                BackupType? newType = _inputService.AskBackupTypeWithCurrentValue(LocalizationKey.menu_job_update_type, job.Type);
                if (newType != null) job.Type = newType.Value;
                break;
        }

        UpdateJob(job, onBackToMainMenu);
    }

    /// <summary>
    /// Persists the current job updates.
    /// </summary>
    /// <param name="job">Job to save.</param>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    private void SaveJobUpdate(BackupJob job, Action onBackToMainMenu)
    {
        try
        {
            _backupApplicationService.UpdateJob(job);
            _messageService.WriteWithParams(LocalizationKey.backupjob_updated_named, [job.Name]);
            _editSessionService.Clear();
        }
        catch (Exception e)
        {
            _messageService.ShowError(e);
        }

        _menuService.WaitForUser();
        ShowJobsList(onBackToMainMenu);
    }

    /// <summary>
    /// Exits update mode, handling unsaved changes if needed.
    /// </summary>
    /// <param name="job">Job currently being edited.</param>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    private void ExitJobUpdate(BackupJob job, Action onBackToMainMenu)
    {
        if (_editSessionService.HasPendingChanges(job))
        {
            int selectedOption = ShowUnsavedChangesMenu();
            if (selectedOption == 0)
            {
                try
                {
                    _backupApplicationService.UpdateJob(job);
                    _messageService.WriteWithParams(LocalizationKey.backupjob_updated_named, [job.Name]);
                    _editSessionService.Clear();
                    _menuService.WaitForUser();
                    ShowJobDetails(job, onBackToMainMenu);
                }
                catch (Exception e)
                {
                    _messageService.ShowError(e);
                    _menuService.WaitForUser();
                    UpdateJob(job, onBackToMainMenu);
                }

                return;
            }

            if (selectedOption == 1)
            {
                _editSessionService.Restore(job);
                _editSessionService.Clear();
                ShowJobDetails(job, onBackToMainMenu);
                return;
            }

            UpdateJob(job, onBackToMainMenu);
            return;
        }

        _editSessionService.Clear();
        ShowJobDetails(job, onBackToMainMenu);
    }

    /// <summary>
    /// Deletes the selected job after confirmation.
    /// </summary>
    /// <param name="job">Job to delete.</param>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    private void DeleteJob(BackupJob job, Action onBackToMainMenu)
    {
        _consoleAdapter.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_job_delete);

        _messageService.Write(LocalizationKey.backupjob_name, false);
        _consoleAdapter.WriteLine($": {job.Name}");
        _consoleAdapter.WriteLine();

        _messageService.Write(LocalizationKey.backupjob_delete_confirm);
        var key = _consoleAdapter.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Y || key.Key == ConsoleKey.Enter)
        {
            try
            {
                _backupApplicationService.RemoveJob(job.Id);
                _messageService.WriteWithParams(LocalizationKey.backupjob_deleted_named, [job.Name]);
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex);
            }
        }
        else
        {
            ShowJobDetails(job, onBackToMainMenu);
            return;
        }

        _menuService.WaitForUser();
        ShowJobsList(onBackToMainMenu);
    }

    /// <summary>
    /// Displays the unsaved changes decision menu.
    /// </summary>
    /// <returns>Selected option index.</returns>
    private int ShowUnsavedChangesMenu()
    {
        LocalizationKey[] options =
        {
            LocalizationKey.job_update_unsaved_save_and_quit,
            LocalizationKey.job_update_unsaved_discard_and_quit,
            LocalizationKey.back
        };

        Action renderHeader = () =>
        {
            _messageService.Write(LocalizationKey.job_update_unsaved_question);
            _consoleAdapter.WriteLine();
        };

        return _menuService.ShowMenu(options, LocalizationKey.job_update_unsaved_title, renderHeader);
    }
}
