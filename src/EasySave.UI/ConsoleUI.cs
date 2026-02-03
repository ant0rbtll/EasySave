using EasySave.Application;
using EasySave.Localization;
using EasySave.Core;
using EasySave.UI.Menu;

namespace EasySave.UI;

/// <summary>
/// Actions and display of the application with the console
/// </summary>
public class ConsoleUI
{

    private readonly BackupAppService _backupAppService;
    public ILocalizationService LocalizationService { get; }
    private readonly MenuService _menuService;
    private readonly MenuFactory _menuFactory;

    public ConsoleUI(BackupAppService backupAppService)
    {
        _backupAppService = backupAppService;
        LocalizationService = new LocalizationService();
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

        var input = new global::System.Text.StringBuilder();
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
                if (input.Length == 0 || string.IsNullOrWhiteSpace(input.ToString()))
                {
                    ShowMessage(LocalizationKey.input_string_invalid, false);
                    ShowMessage(LocalizationKey.input_escape_to_cancel, false);
                    Console.Write(" : ");
                    input.Clear();
                }
                else
                {
                    break;
                }
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input.Remove(input.Length - 1, 1);
                Console.Write("\b \b"); // Erase the character on screen
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                input.Append(keyInfo.KeyChar);
                Console.Write(keyInfo.KeyChar);
            }
        }
        while (true);

        return input.ToString();
    }

    /// <inheritdoc />
    public int? AskInt(LocalizationKey key)
    {
        ShowMessage(key, false);
        ShowMessage(LocalizationKey.input_escape_to_cancel, false);
        Console.Write(" : ");
        var input = new global::System.Text.StringBuilder();
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
                if (int.TryParse(input.ToString(), out int numberInput))
                {
                    return numberInput;
                }
                else
                {
                    ShowMessage(LocalizationKey.input_number_invalid, false);
                    ShowMessage(LocalizationKey.input_escape_to_cancel, false);
                    Console.Write(" : " );
                    input.Clear();
                }
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input.Remove(input.Length - 1, 1);
                Console.Write("\b \b");
            }
            else if (char.IsDigit(keyInfo.KeyChar) || (keyInfo.KeyChar == '-' && input.Length == 0))
            {
                input.Append(keyInfo.KeyChar);
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
        _menuService.ShowMenuWithActions(menuConfig.Items, menuConfig.Actions, menuConfig.Label);
    }

    /// <summary>
    /// The menu to change the app language
    /// </summary>
    public void ShowChangeLocale()
    {
        var menuConfig = _menuFactory.CreateLocaleMenu();
        _menuService.ShowMenuWithActions(menuConfig.Items, menuConfig.Actions, menuConfig.Label);
    }

    /// <summary>
    /// The action of changing the language
    /// </summary>
    /// <param name="locale"></param>
    public void ChangeLocale(string locale)
    {
        LocalizationService.Culture = locale;

        MainMenu();
    }

    /// <summary>
    /// The main menu of the application
    /// </summary>
    public void MainMenu()
    {
        var menuConfig = _menuFactory.CreateMainMenu();
        _menuService.ShowMenuWithActions(menuConfig.Items, menuConfig.Actions, menuConfig.Label);

    }

    /// <summary>
    /// Quit the application
    /// </summary>
    public void Quit()
    {
        Console.Clear();
    }
}
