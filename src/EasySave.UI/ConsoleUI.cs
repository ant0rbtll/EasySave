using EasySave.Application;
using EasySave.Core;
using EasySave.Localization;
using EasySave.Persistence;
using EasySave.Configuration;
using EasySave.UI.Menu;

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
    private int? _editingJobId;
    private BackupJob? _editingJobSnapshot;
    public ILocalizationService LocalizationService { get; }
    private readonly MenuService _menuService;
    private readonly MenuFactory _menuFactory;
    private readonly CommandLineParser _parser;
    private readonly ErrorManager _errorManager;

    public ConsoleUI(BackupApplicationService backupApplicationService, IUserPreferencesRepository preferencesRepository, IPathProvider pathProvider, CommandLineParser parser)
    {
        _backupApplicationService = backupApplicationService;
        _preferencesRepository = preferencesRepository;
        _pathProvider = pathProvider;
        LocalizationService = new LocalizationService();
        _parser = parser;
        _errorManager = new ErrorManager();

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
        _menuFactory = new MenuFactory(this, _backupApplicationService);
    }

    /// <inheritdoc />
    private void ShowMessage(LocalizationKey key, bool writeLine = true)
    {
        string message = LocalizationService.TranslateText(key);
        if (writeLine) Console.WriteLine(message);
        else Console.Write(message);
    }

    private void ShowMessageParam(LocalizationKey key, string[] parameters, bool writeLine = true)
    {
        string message = LocalizationService.TranslateTextWithParams(key, parameters);
        if (writeLine) Console.WriteLine(message);
        else Console.Write(message);
    }

    /// <inheritdoc />
    public void ShowError(Exception e)
    {
        Console.WriteLine("");
        Console.ForegroundColor = ConsoleColor.Red;
        ShowMessage(LocalizationKey.error);
        var messageKey = e.Message;
        if (e.Data.Contains("errorKey") && e.Data["errorKey"] is string dataKey)
        {
            messageKey = dataKey;
        }

        if (_errorManager.TryGetMessage(messageKey, out var key))
        {
            ShowMessageParam(key,
                e.Data.Keys
                .Cast<string>()
                .Where(k => !string.Equals(k, "errorKey", StringComparison.Ordinal))
                .OrderBy(k => k)
                .Select(k => e.Data[k]?.ToString() ?? string.Empty)
                .ToArray()
            );
        }
        else
        {
            Console.WriteLine(e.Message);
        }
        Console.ResetColor();
    }

    /// <inheritdoc />
    public string? AskString(LocalizationKey key)
    {
        ShowMessage(key, false);
        ShowMessage(LocalizationKey.input_escape_to_cancel, false);
        Console.Write(" : ");

        string input = "";
        ConsoleKeyInfo keyInfo;

        do
        {
            keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                return null;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    ShowMessage(LocalizationKey.input_string_invalid, false);
                    ShowMessage(LocalizationKey.input_escape_to_cancel, false);
                    Console.Write(" : ");
                    input = "";
                }
                else
                {
                    break;
                }
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input = input.Substring(0, input.Length - 1);
                Console.Write("\b \b"); // Erase the character on screen
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                input += keyInfo.KeyChar;
                Console.Write(keyInfo.KeyChar);
            }
        }
        while (true);

        return input;
    }

    /// <inheritdoc />
    public int? AskInt(LocalizationKey key)
    {
        ShowMessage(key, false);
        ShowMessage(LocalizationKey.input_escape_to_cancel, false);
        Console.Write(" : ");
        string input = "";
        ConsoleKeyInfo keyInfo;

        do
        {
            keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                return null;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                if (int.TryParse(input, out int numberInput))
                {
                    return numberInput;
                }
                else
                {
                    ShowMessage(LocalizationKey.input_number_invalid, false);
                    ShowMessage(LocalizationKey.input_escape_to_cancel, false);
                    Console.Write(" : ");
                    input = "";
                }
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input = input.Substring(0, input.Length - 1);
                Console.Write("\b \b");
            }
            else if (char.IsDigit(keyInfo.KeyChar) || (keyInfo.KeyChar == '-' && input.Length == 0))
            {
                input += keyInfo.KeyChar;
                Console.Write(keyInfo.KeyChar);
            }
        }
        while (true);
    }

    private string? AskStringWithCurrentValue(LocalizationKey key, string currentValue)
    {
        ShowMessage(key, false);
        ShowMessage(LocalizationKey.input_escape_to_cancel, false);
        ShowMessageParam(LocalizationKey.input_enter_to_keep_current, new[] { currentValue }, false);
        Console.Write(" : ");

        string input = "";
        ConsoleKeyInfo keyInfo;

        do
        {
            keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                return null;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                if (input.Length == 0)
                {
                    return currentValue;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    ShowMessage(LocalizationKey.input_string_invalid, false);
                    ShowMessage(LocalizationKey.input_escape_to_cancel, false);
                    ShowMessageParam(LocalizationKey.input_enter_to_keep_current, new[] { currentValue }, false);
                    Console.Write(" : ");
                    input = "";
                }
                else
                {
                    return input;
                }
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input = input.Substring(0, input.Length - 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                input += keyInfo.KeyChar;
                Console.Write(keyInfo.KeyChar);
            }
        }
        while (true);
    }

    private int? AskIntWithCurrentValue(LocalizationKey key, int currentValue)
    {
        ShowMessage(key, false);
        ShowMessage(LocalizationKey.input_escape_to_cancel, false);
        ShowMessageParam(LocalizationKey.input_enter_to_keep_current, new[] { currentValue.ToString() }, false);
        Console.Write(" : ");

        string input = "";
        ConsoleKeyInfo keyInfo;

        do
        {
            keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                return null;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                if (input.Length == 0)
                {
                    return currentValue;
                }

                if (int.TryParse(input, out int numberInput))
                {
                    return numberInput;
                }

                ShowMessage(LocalizationKey.input_number_invalid, false);
                ShowMessage(LocalizationKey.input_escape_to_cancel, false);
                ShowMessageParam(LocalizationKey.input_enter_to_keep_current, new[] { currentValue.ToString() }, false);
                Console.Write(" : ");
                input = "";
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input = input.Substring(0, input.Length - 1);
                Console.Write("\b \b");
            }
            else if (char.IsDigit(keyInfo.KeyChar) || (keyInfo.KeyChar == '-' && input.Length == 0))
            {
                input += keyInfo.KeyChar;
                Console.Write(keyInfo.KeyChar);
            }
        }
        while (true);
    }

    /// <inheritdoc />
    public BackupType? AskBackupType(LocalizationKey key)
    {
        ShowMessage(LocalizationKey.backupjob_type_list);
        var values = Enum.GetValues(typeof(BackupType)).Cast<BackupType>().ToArray();
        for (int i = 0; i < values.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {values[i]}");
        }

        ShowMessage(key);
        while (true)
        {
            int? backupTypeInput = AskInt(LocalizationKey.user_choice);
            if (backupTypeInput == null)
            {
                return null;
            }

            int choice = backupTypeInput.Value;
            if (choice >= 1 && choice <= values.Length)
            {
                return values[choice - 1];
            }

            ShowMessage(LocalizationKey.input_backuptype_invalid);
        }
    }

    private BackupType? AskBackupTypeWithCurrentValue(LocalizationKey key, BackupType currentType)
    {
        ShowMessage(LocalizationKey.backupjob_type_list);
        var values = Enum.GetValues(typeof(BackupType)).Cast<BackupType>().ToArray();
        for (int i = 0; i < values.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {values[i]}");
        }

        ShowMessage(key);
        int currentChoice = Array.IndexOf(values, currentType) + 1;

        while (true)
        {
            int? backupTypeInput = AskIntWithCurrentValue(LocalizationKey.user_choice, currentChoice);
            if (backupTypeInput == null)
            {
                return null;
            }

            int choice = backupTypeInput.Value;
            if (choice >= 1 && choice <= values.Length)
            {
                return values[choice - 1];
            }

            ShowMessage(LocalizationKey.input_backuptype_invalid);
        }
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
            ShowMessage(LocalizationKey.backupjob_created);
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
                BackupJob? job = _backupApplicationService.GetJobById(backupIndex.Value);
                if (job == null)
                {
                    ShowMessage(LocalizationKey.backupjob_id_not_found);
                    continue;
                }

                ShowMessage(LocalizationKey.backup_saving);
                _backupApplicationService.RunJobById(backupIndex.Value);
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
                BackupJob? job = _backupApplicationService.GetJobById(backupIndex.Value);
                if (job == null)
                {
                    ShowMessage(LocalizationKey.backupjob_id_not_found);
                    continue;
                }
                _backupApplicationService.RemoveJob(backupIndex.Value);
                ShowMessage(LocalizationKey.backupjob_deleted);
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
        var menuConfig = _menuFactory.CreateParamsMenu();
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Displays the language selection menu.
    /// </summary>
    public void ShowChangeLocale()
    {
        var menuConfig = _menuFactory.CreateLocaleMenu();
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
        var menuConfig = _menuFactory.CreateLogFormatMenu();
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

    public string BuildLogFormatMenuItem(LogFormat format)
    {
        return GetLogFormatLabel(format);
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
        var menuConfig = _menuFactory.CreateMainMenu();
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
        var menuConfig = _menuFactory.CreateJobsListMenu();
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

        var menuConfig = _menuFactory.CreateJobDetailsMenu(job, renderJobDetails);
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
        Console.Clear();
        BeginOrRefreshEditSession(job);
        var menuConfig = _menuFactory.CreateJobUpdateMenu(job);
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
            ShowMessage(LocalizationKey.backupjob_updated);
            ClearEditSession();
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
        if (HasPendingJobChanges(job))
        {
            int selectedOption = ShowUnsavedChangesMenu();
            if (selectedOption == 0)
            {
                try
                {
                    _backupApplicationService.UpdateJob(job);
                    ShowMessage(LocalizationKey.backupjob_updated);
                    ClearEditSession();
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
                RestoreJobFromSnapshot(job);
                ClearEditSession();
                ShowJobDetails(job);
                return;
            }

            UpdateJob(job);
            return;
        }

        ClearEditSession();
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
    /// <param name="args">The args of the command</param>
    internal void RunFromArgs(string[] args)
    {
        try
        {
            var jobs = _parser.Parse(args);
            _backupApplicationService.RunJobsByIds(jobs);
        }
        catch (Exception e)
        {
            ShowError(e);
        }
        _menuService.WaitForUser();
    }

    private void BeginOrRefreshEditSession(BackupJob job)
    {
        if (_editingJobId == job.Id && _editingJobSnapshot != null)
        {
            return;
        }

        _editingJobId = job.Id;
        _editingJobSnapshot = CloneJob(job);
    }

    private bool HasPendingJobChanges(BackupJob job)
    {
        if (_editingJobId != job.Id || _editingJobSnapshot == null)
        {
            return false;
        }

        return !AreJobsEqual(job, _editingJobSnapshot);
    }

    private void ClearEditSession()
    {
        _editingJobId = null;
        _editingJobSnapshot = null;
    }

    private void RestoreJobFromSnapshot(BackupJob job)
    {
        if (_editingJobId != job.Id || _editingJobSnapshot == null)
        {
            return;
        }

        job.Name = _editingJobSnapshot.Name;
        job.Source = _editingJobSnapshot.Source;
        job.Destination = _editingJobSnapshot.Destination;
        job.Type = _editingJobSnapshot.Type;
    }

    private static BackupJob CloneJob(BackupJob job)
    {
        return new BackupJob
        {
            Id = job.Id,
            Name = job.Name,
            Source = job.Source,
            Destination = job.Destination,
            Type = job.Type
        };
    }

    private static bool AreJobsEqual(BackupJob first, BackupJob second)
    {
        return first.Id == second.Id
            && string.Equals(first.Name, second.Name, StringComparison.Ordinal)
            && string.Equals(first.Source, second.Source, StringComparison.Ordinal)
            && string.Equals(first.Destination, second.Destination, StringComparison.Ordinal)
            && first.Type == second.Type;
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
