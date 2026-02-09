using EasySave.Application;
using EasySave.Core;
using EasySave.Localization;
using EasySave.Persistence;
using EasySave.Configuration;
using EasySave.UI.Menu;
using EasySave.UI.Services;

namespace EasySave.UI;

/// <summary>
/// Console-based user interface for the EasySave application.
/// </summary>
public class ConsoleUI
{
    private readonly BackupApplicationService _backupApplicationService;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly UserPreferences _userPreferences;
    private readonly IPathProvider _pathProvider;
    private readonly LogFormat _activeLogFormat;
    private string _activeLogDirectory = string.Empty;
    private bool _isUsingDefaultLogDirectory;
    public ILocalizationService LocalizationService { get; }
    private readonly MenuService _menuService;
    private readonly MenuFactory _menuFactory;
    private readonly CommandLineParser _parser;
    private readonly ConsoleMessageService _messageService;
    private readonly ConsoleInputService _inputService;
    private readonly JobEditSessionService _editSessionService;

    public ConsoleUI(BackupApplicationService backupApplicationService, IUserPreferencesRepository preferencesRepository, IPathProvider pathProvider, CommandLineParser parser)
    {
        _backupApplicationService = backupApplicationService;
        _preferencesRepository = preferencesRepository;
        _pathProvider = pathProvider;
        LocalizationService = new LocalizationService();
        _parser = parser;

        _userPreferences = _preferencesRepository.Load();
        _activeLogFormat = _userPreferences.LogFormat;
        var language = _userPreferences.Language;

        ApplyLogDirectoryPreference(_userPreferences.LogDirectory);

        if (string.IsNullOrWhiteSpace(language) || !LocalizationService.AllCultures.ContainsKey(language))
        {
            language = "fr";
            _userPreferences.Language = language;
            _preferencesRepository.Save(_userPreferences);
        }

        LocalizationService.Culture = language;

        _menuService = new MenuService(LocalizationService);
        _menuFactory = new MenuFactory();
        _messageService = new ConsoleMessageService(LocalizationService, new ErrorManager());
        _inputService = new ConsoleInputService(_messageService);
        _editSessionService = new JobEditSessionService();
    }

    private void ShowMessage(LocalizationKey key, bool writeLine = true)
    {
        _messageService.Write(key, writeLine);
    }

    private void ShowMessageParam(LocalizationKey key, string[] parameters, bool writeLine = true)
    {
        _messageService.WriteWithParams(key, parameters, writeLine);
    }

    public void ShowError(Exception e)
    {
        _messageService.ShowError(e);
    }

    public string? AskString(LocalizationKey key)
    {
        return _inputService.AskString(key);
    }

    public int? AskInt(LocalizationKey key)
    {
        return _inputService.AskInt(key);
    }

    private string? AskStringWithCurrentValue(LocalizationKey key, string currentValue)
    {
        return _inputService.AskStringWithCurrentValue(key, currentValue);
    }

    private int? AskIntWithCurrentValue(LocalizationKey key, int currentValue)
    {
        return _inputService.AskIntWithCurrentValue(key, currentValue);
    }

    public BackupType? AskBackupType(LocalizationKey key)
    {
        return _inputService.AskBackupType(key);
    }

    private BackupType? AskBackupTypeWithCurrentValue(LocalizationKey key, BackupType currentType)
    {
        return _inputService.AskBackupTypeWithCurrentValue(key, currentType);
    }

    /// <summary>
    /// Collects backup job information from the user and creates a new job.
    /// </summary>
    public void CreateBackupJob()
    {
        _menuService.DisplayLabel(LocalizationKey.menu_create);

        string? nameJob = AskString(LocalizationKey.backupjob_create_name);
        if (nameJob == null) { MainMenu(); return; }

        string? sourceJob = AskString(LocalizationKey.backupjob_create_source);
        if (sourceJob == null) { MainMenu(); return; }

        string? destinationJob = AskString(LocalizationKey.backupjob_create_destination);
        if (destinationJob == null) { MainMenu(); return; }

        BackupType? backupTypeJob = AskBackupType(LocalizationKey.backupjob_create_type);
        if (backupTypeJob == null) { MainMenu(); return; }

        // send to service
        try
        {
            _backupApplicationService.CreateJob(nameJob, sourceJob, destinationJob, backupTypeJob.Value);
            ShowMessageParam(LocalizationKey.backupjob_created_named, [nameJob]);
        }
        catch (Exception e)
        {
            ShowError(e);
        }
        _menuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Displays the list of backup jobs in the console.
    /// </summary>
    private void DisplayJobsList()
    {
        try
        {
            List<BackupJob> backupJobList = _backupApplicationService.GetAllJobs();
            foreach (BackupJob job in backupJobList)
            {
                Console.WriteLine(job.Id + " - " + job.Name);
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
    /// Displays all backup jobs and waits for user input.
    /// </summary>
    public void SeeSaveList()
    {
        _menuService.DisplayLabel(LocalizationKey.menu_list);
        DisplayJobsList();
        _menuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Prompts the user to select and run a backup job.
    /// </summary>
    public void SaveJob()
    {
        _menuService.DisplayLabel(LocalizationKey.menu_save);
        DisplayJobsList();
        Console.WriteLine();

        while (true)
        {
            int? backupIndex = AskInt(LocalizationKey.ask_backupjob_save);
            if (backupIndex == null) { MainMenu(); return; }
            try
            {
                BackupJob? job = _backupApplicationService.GetJob(backupIndex.Value);
                if (job == null)
                {
                    ShowMessage(LocalizationKey.backupjob_id_not_found);
                    continue;
                }

                ShowMessage(LocalizationKey.backup_saving);
                _backupApplicationService.RunJob(backupIndex.Value);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            break;
        }

        _menuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Prompts the user to select and delete a backup job.
    /// </summary>
    public void DeleteBackupJob()
    {
        _menuService.DisplayLabel(LocalizationKey.menu_delete);
        DisplayJobsList();
        Console.WriteLine();

        while (true)
        {
            int? backupIndex = AskInt(LocalizationKey.ask_backupjob_delete);
            if (backupIndex == null) { MainMenu(); return; }

            try
            {
                BackupJob? job = _backupApplicationService.GetJob(backupIndex.Value);
                if (job == null)
                {
                    ShowMessage(LocalizationKey.backupjob_id_not_found);
                    continue;
                }
                _backupApplicationService.RemoveJob(backupIndex.Value);
                ShowMessageParam(LocalizationKey.backupjob_deleted_named, new[] { job.Name });
                break;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }

        }

        _menuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Displays the application settings menu.
    /// </summary>
    public void ConfigureParams()
    {
        var menuConfig = _menuFactory.CreateParamsMenu(
            ShowChangeLocale,
            ShowChangeLogDirectory,
            ShowChangeLogFormat,
            MainMenu,
            RenderSettingsHeader);
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Displays the language selection menu.
    /// </summary>
    public void ShowChangeLocale()
    {
        var menuConfig = _menuFactory.CreateLocaleMenu(
            LocalizationService.AllCultures,
            ChangeLocale,
            ConfigureParams,
            RenderLocaleHeader);
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Displays the log directory configuration menu.
    /// </summary>
    public void ShowChangeLogDirectory()
    {
        Console.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_params_log_path);
        DisplayActiveLogDirectoryStatus();
        Console.WriteLine();

        string? input = AskString(LocalizationKey.ask_log_path);
        if (input == null)
        {
            ConfigureParams();
            return;
        }

        if (input.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            ChangeLogDirectory(null);
            return;
        }

        if (!IsValidPath(input))
        {
            ShowMessage(LocalizationKey.log_path_invalid);
            _menuService.WaitForUser();
            ConfigureParams();
            return;
        }

        ChangeLogDirectory(input);
    }

    /// <summary>
    /// Changes the application language and persists the preference.
    /// </summary>
    /// <param name="locale">The culture code to set (e.g., "fr", "en").</param>
    public void ChangeLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale) || !LocalizationService.AllCultures.ContainsKey(locale))
        {
            locale = "fr";
        }

        LocalizationService.Culture = locale;

        // Update cached preferences and save
        _userPreferences.Language = locale;
        _preferencesRepository.Save(_userPreferences);

        MainMenu();
    }

    private void ChangeLogDirectory(string? directory)
    {
        ApplyLogDirectoryPreference(directory);
        _userPreferences.LogDirectory = string.IsNullOrWhiteSpace(directory) ? null : directory;
        _preferencesRepository.Save(_userPreferences);

        if (string.IsNullOrWhiteSpace(directory))
        {
            ShowMessage(LocalizationKey.log_path_reset);
        }
        else
        {
            ShowMessage(LocalizationKey.log_path_updated);
        }

        _menuService.WaitForUser();
        ConfigureParams();
    }
    /// <summary>
    /// Displays the log format selection menu.
    /// </summary>
    public void ShowChangeLogFormat()
    {
        var menuConfig = _menuFactory.CreateLogFormatMenu(
            GetLogFormatLabel(LogFormat.Json),
            GetLogFormatLabel(LogFormat.Xml),
            _messageService.Translate(LocalizationKey.back),
            () => ChangeLogFormat(LogFormat.Json),
            () => ChangeLogFormat(LogFormat.Xml),
            ConfigureParams,
            RenderLogFormatHeader);
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Changes the log format and persists the preference.
    /// </summary>
    /// <param name="format">The log format to set.</param>
    public void ChangeLogFormat(LogFormat format)
    {
        _userPreferences.LogFormat = format;
        _preferencesRepository.Save(_userPreferences);

        ShowMessage(LocalizationKey.log_format_updated);
        ShowMessage(LocalizationKey.log_format_restart_required);
        _menuService.WaitForUser();
        ConfigureParams();
    }

    public void RenderSettingsHeader()
    {
        ShowMessageParam(LocalizationKey.settings_current_language, new[] { GetCurrentLanguageLabel() });
        ShowMessageParam(LocalizationKey.settings_log_format_active, new[] { GetLogFormatLabel(_activeLogFormat) });

        if (_userPreferences.LogFormat != _activeLogFormat)
        {
            ShowMessageParam(LocalizationKey.settings_log_format_pending, new[] { GetLogFormatLabel(_userPreferences.LogFormat) });
        }

        DisplayActiveLogDirectoryStatus();
        Console.WriteLine();
    }

    public void RenderLocaleHeader()
    {
        ShowMessageParam(LocalizationKey.settings_current_language, new[] { GetCurrentLanguageLabel() });
        Console.WriteLine();
    }

    public void RenderLogFormatHeader()
    {
        ShowMessageParam(LocalizationKey.settings_log_format_active, new[] { GetLogFormatLabel(_activeLogFormat) });

        if (_userPreferences.LogFormat != _activeLogFormat)
        {
            ShowMessageParam(LocalizationKey.settings_log_format_pending, new[] { GetLogFormatLabel(_userPreferences.LogFormat) });
        }

        Console.WriteLine();
    }

    private void ApplyLogDirectoryPreference(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            _pathProvider.SetLogDirectoryOverride(null);
            SetDefaultLogDirectoryAsActive();
            return;
        }

        if (!IsValidPath(directory))
        {
            try
            {
                Console.Error.WriteLine($"Invalid log directory preference '{directory}'. Reverting to default log directory.");
            }
            catch
            {
                // Best-effort notification only.
            }
            _pathProvider.SetLogDirectoryOverride(null);
            SetDefaultLogDirectoryAsActive();
            return;
        }

        _pathProvider.SetLogDirectoryOverride(directory);
        _activeLogDirectory = ResolveLogDirectoryCandidate(directory);
        _isUsingDefaultLogDirectory = false;
    }

    private static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return false;
        }

        try
        {
            string candidate = ResolveLogDirectoryCandidate(path);
            Directory.CreateDirectory(candidate);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static string ResolveLogDirectoryCandidate(string directory)
    {
        var trimmed = directory.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            return trimmed;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, trimmed));
    }

    private void SetDefaultLogDirectoryAsActive()
    {
        _activeLogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        _isUsingDefaultLogDirectory = true;
    }

    private void DisplayActiveLogDirectoryStatus()
    {
        if (_isUsingDefaultLogDirectory)
        {
            ShowMessageParam(LocalizationKey.settings_log_directory_active_default, new[] { _activeLogDirectory });
            return;
        }

        ShowMessageParam(LocalizationKey.settings_log_directory_active_custom, new[] { _activeLogDirectory });
    }

    private string GetCurrentLanguageLabel()
    {
        if (LocalizationService.AllCultures.TryGetValue(LocalizationService.Culture, out var cultureKey))
        {
            return LocalizationService.TranslateText(cultureKey);
        }

        return LocalizationService.Culture;
    }

    private string GetLogFormatLabel(LogFormat format)
    {
        return format switch
        {
            LogFormat.Xml => LocalizationService.TranslateText(LocalizationKey.log_format_xml),
            _ => LocalizationService.TranslateText(LocalizationKey.log_format_json)
        };
    }

    /// <summary>
    /// Displays the main menu of the application.
    /// </summary>
    public void MainMenu()
    {
        var currentJobCount = _backupApplicationService.GetAllJobs().Count;
        var menuConfig = _menuFactory.CreateMainMenu(
            currentJobCount,
            CreateBackupJob,
            ShowJobsList,
            ConfigureParams,
            Quit);
        _menuService.ShowMenuWithActions(menuConfig);

    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    public void Quit()
    {
        Console.Clear();
    }

    /// <summary>
    /// Shows interactive list of backup jobs
    /// </summary>
    public void ShowJobsList()
    {
        var jobs = _backupApplicationService.GetAllJobs();
        var menuConfig = _menuFactory.CreateJobsListMenu(
            jobs,
            _messageService.Translate(LocalizationKey.back),
            ShowJobDetails,
            MainMenu);
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Shows details of a specific backup job with action options
    /// </summary>
    public void ShowJobDetails(BackupJob job)
    {
        Action renderJobDetails = () =>
        {
            ShowMessage(LocalizationKey.backupjob_id, false);
            Console.WriteLine($": {job.Id}");

            ShowMessage(LocalizationKey.backupjob_name, false);
            Console.WriteLine($": {job.Name}");

            ShowMessage(LocalizationKey.backupjob_source, false);
            Console.WriteLine($": {job.Source}");

            ShowMessage(LocalizationKey.backupjob_destination, false);
            Console.WriteLine($": {job.Destination}");

            ShowMessage(LocalizationKey.backupjob_type, false);
            Console.WriteLine($": {job.Type}");

            Console.WriteLine();
        };

        var menuConfig = _menuFactory.CreateJobDetailsMenu(
            job,
            RunJob,
            UpdateJob,
            DeleteJob,
            ShowJobsList,
            renderJobDetails);
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Runs a backup job
    /// </summary>
    public void RunJob(BackupJob job)
    {
        Console.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_job_run);

        ShowMessage(LocalizationKey.backupjob_running);
        try
        {
            _backupApplicationService.RunJob(job.Id);
            ShowMessageParam(LocalizationKey.backupjob_completed_named, new[] { job.Name });
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
        Console.Clear();
        _editSessionService.BeginOrRefresh(job);
        var menuConfig = _menuFactory.CreateJobUpdateMenu(
            job,
            UpdateJobField,
            SaveJobUpdate,
            ExitJobUpdate);
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Updates a specific field of a backup job
    /// </summary>
    public void UpdateJobField(BackupJob job, string field)
    {
        Console.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_job_update);

        switch (field)
        {
            case "name":
                string? newName = AskStringWithCurrentValue(LocalizationKey.menu_job_update_name, job.Name);
                if (newName != null) job.Name = newName;
                break;
            case "source":
                string? newSource = AskStringWithCurrentValue(LocalizationKey.menu_job_update_source, job.Source);
                if (newSource != null) job.Source = newSource;
                break;
            case "destination":
                string? newDestination = AskStringWithCurrentValue(LocalizationKey.menu_job_update_destination, job.Destination);
                if (newDestination != null) job.Destination = newDestination;
                break;
            case "type":
                BackupType? newType = AskBackupTypeWithCurrentValue(LocalizationKey.menu_job_update_type, job.Type);
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
            ShowMessageParam(LocalizationKey.backupjob_updated_named, new[] { job.Name });
            _editSessionService.Clear();
        }
        catch (Exception e)
        {
            ShowError(e);
        }
        _menuService.WaitForUser();
        ShowJobsList();
    }

    public void ExitJobUpdate(BackupJob job)
    {
        if (_editSessionService.HasPendingChanges(job))
        {
            int selectedOption = ShowUnsavedChangesMenu();
            if (selectedOption == 0)
            {
                try
                {
                    _backupApplicationService.UpdateJob(job);
                    ShowMessageParam(LocalizationKey.backupjob_updated_named, new[] { job.Name });
                    _editSessionService.Clear();
                    _menuService.WaitForUser();
                    ShowJobDetails(job);
                }
                catch (Exception e)
                {
                    ShowError(e);
                    _menuService.WaitForUser();
                    UpdateJob(job);
                }

                return;
            }

            if (selectedOption == 1)
            {
                _editSessionService.Restore(job);
                _editSessionService.Clear();
                ShowJobDetails(job);
                return;
            }

            UpdateJob(job);
            return;
        }

        _editSessionService.Clear();
        ShowJobDetails(job);
    }

    /// <summary>
    /// Deletes a backup job with confirmation
    /// </summary>
    public void DeleteJob(BackupJob job)
    {
        Console.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_job_delete);

        ShowMessage(LocalizationKey.backupjob_name, false);
        Console.WriteLine($": {job.Name}");
        Console.WriteLine();

        ShowMessage(LocalizationKey.backupjob_delete_confirm);
        var key = Console.ReadKey(intercept: true);
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
            ShowMessageParam(LocalizationKey.backupjob_deleted_named, new[] { job.Name });
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
    /// <param name="args">The args of the command</param>
    internal void RunFromArgs(string[] args)
    {
        try
        {
            var jobs = _parser.Parse(args);
            _backupApplicationService.RunJobs(jobs);
        }
        catch (Exception e)
        {
            ShowError(e);
        }
        _menuService.WaitForUser();
    }

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
            ShowMessage(LocalizationKey.job_update_unsaved_question);
            Console.WriteLine();
        };

        return _menuService.ShowMenu(options, LocalizationKey.job_update_unsaved_title, renderHeader);
    }
}
