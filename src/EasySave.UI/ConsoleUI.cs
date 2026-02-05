using EasySave.Application;
using EasySave.Localization;
using EasySave.Core;
using EasySave.UI.Menu;
using EasySave.Persistence;
using EasySave.Configuration;

namespace EasySave.UI;

/// <summary>
/// Actions and display of the application with the console
/// </summary>
public class ConsoleUI
{

    private readonly BackupAppService _backupAppService;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly UserPreferences _userPreferences;
    private readonly IPathProvider _pathProvider;
    public ILocalizationService LocalizationService { get; }
    private readonly MenuService _menuService;
    private readonly MenuFactory _menuFactory;
    private readonly CommandLineParser _parser;

    public ConsoleUI(
        BackupAppService backupAppService,
        IUserPreferencesRepository preferencesRepository,
        IPathProvider pathProvider,
        CommandLineParser parser)
    {
        _backupAppService = backupAppService;
        _preferencesRepository = preferencesRepository;
        _pathProvider = pathProvider;
        LocalizationService = new LocalizationService();
        _parser = parser;
        

        _userPreferences = _preferencesRepository.Load();
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
        _menuFactory = new MenuFactory(this, _backupAppService);
    }

    /// <inheritdoc />
    private void ShowMessage(LocalizationKey key, bool writeLine = true)
    {
        string message = LocalizationService.TranslateText(key);
        if (writeLine) Console.WriteLine(message);
        else Console.Write(message);
    }

    // private void ShowMessageParam(LocalizationKey key, Dictionary<string, string> parameters, bool writeLine = true)
    // {
    //     string message = LocalizationService.TranslateText(key, parameters);
    //     if (writeLine) Console.WriteLine(message);
    //     else Console.Write(message);
    // }

    /// <inheritdoc />
    public void ShowError(LocalizationKey key)
    {
        Console.Error.WriteLine(LocalizationService.TranslateText(key));
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
                    Console.Write(" : " );
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

    /// <summary>
    /// Gather a save's informations and create a BackupJob
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
        _backupAppService.CreateJob(nameJob, sourceJob, destinationJob, backupTypeJob.Value);
        ShowMessage(LocalizationKey.backupjob_created);
        _menuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Display the list of BackupJobs
    /// </summary>
    private void DisplayJobsList()
    {
        List<BackupJob> backupJobList = _backupAppService.GetAllJobs();
        foreach (BackupJob job in backupJobList)
        {
            Console.WriteLine(job.Id + " - " + job.Name);
        }
    }

    /// <summary>
    /// Display the informations of all BackupJob created
    /// </summary>
    public void SeeSaveList()
    {
        _menuService.DisplayLabel(LocalizationKey.menu_list);
        DisplayJobsList();
        _menuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Start a save
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

            BackupJob? job = _backupAppService.GetJobById(backupIndex.Value);
            if (job == null)
            {
                ShowMessage(LocalizationKey.backupjob_id_not_found);
                continue;
            }

            ShowMessage(LocalizationKey.backup_saving);
            _backupAppService.RunJobById(backupIndex.Value);
            break;
        }

        _menuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Delete a BackupJob
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

            BackupJob? job = _backupAppService.GetJobById(backupIndex.Value);
            if (job == null)
            {
                ShowMessage(LocalizationKey.backupjob_id_not_found);
                continue;
            }

            _backupAppService.RemoveJob(backupIndex.Value);
            ShowMessage(LocalizationKey.backupjob_deleted);
            break;
        }

        _menuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// The menu of the app's configuration
    /// </summary>
    public void ConfigureParams()
    {
        var menuConfig = _menuFactory.CreateParamsMenu();
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// The menu to change the app language
    /// </summary>
    public void ShowChangeLocale()
    {
        var menuConfig = _menuFactory.CreateLocaleMenu();
        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// The menu to change the log directory
    /// </summary>
    public void ShowChangeLogDirectory()
    {
        Console.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_params_log_path);

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
            ShowError(LocalizationKey.log_path_invalid);
            _menuService.WaitForUser();
            ConfigureParams();
            return;
        }

        ChangeLogDirectory(input);
    }

    /// <summary>
    /// The action of changing the language
    /// </summary>
    /// <param name="locale"></param>
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

    private void ApplyLogDirectoryPreference(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            _pathProvider.SetLogDirectoryOverride(null);
            return;
        }

        if (!IsValidPath(directory))
        {
            _pathProvider.SetLogDirectoryOverride(null);
            return;
        }

        _pathProvider.SetLogDirectoryOverride(directory);
    }

    private static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.IndexOfAny(Path.GetInvalidPathChars()) < 0;
    }

    /// <summary>
    /// The main menu of the application
    /// </summary>
    public void MainMenu()
    {
        var menuConfig = _menuFactory.CreateMainMenu();
        _menuService.ShowMenuWithActions(menuConfig);

    }

    /// <summary>
    /// Quit the application
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
        _backupAppService.RunJobById(job.Id);
        ShowMessage(LocalizationKey.backupjob_completed);

        _menuService.WaitForUser();
        ShowJobsList();
    }

    /// <summary>
    /// Updates a backup job
    /// </summary>
    public void UpdateJob(BackupJob job)
    {
        Console.Clear();
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
        _backupAppService.UpdateJob(job);
        ShowMessage(LocalizationKey.backupjob_updated);
        _menuService.WaitForUser();
        ShowJobsList();
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
            _backupAppService.RemoveJob(job.Id);
            ShowMessage(LocalizationKey.backupjob_deleted);
            _menuService.WaitForUser();
            ShowJobsList();
        }
        else
        {
            ShowJobDetails(job);
        }
    }
    
    /// <summary>
    /// Run the app through the arguments
    /// </summary>
    /// <param name="args">The args of the command</param>
    internal void RunFromArgs(string[] args)
    {
        try
        {
            var jobs = _parser.Parse(args);
            _backupAppService.RunJobsByIds(jobs);
        }
        catch (Exception)
        {
            ShowMessage(LocalizationKey.backup_error);
        }
        _menuService.WaitForUser();
    }
}
