using EasySave.Application;
using EasySave.Localization;
using EasySave.Core;
using EasySave.UI.Menu;
using YamlDotNet.Serialization;

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
        _menuFactory = new MenuFactory(this);
    }

    /// <inheritdoc />
    private void ShowMessage(LocalizationKey key, bool writeLine = true)
    {
        string message = LocalizationService.TranslateText(key);
        if (writeLine) Console.WriteLine(message);
        else Console.Write(message);
    }

    private void ShowMessageParam(LocalizationKey key, Dictionary<string, string> parameters, bool writeLine = true)
    {
        string message = LocalizationService.TranslateText(key, parameters);
        if (writeLine) Console.WriteLine(message);
        else Console.Write(message);
    }

    /// <inheritdoc />
    public void ShowError(LocalizationKey key)
    {
        Console.Error.WriteLine(LocalizationService.TranslateText(key));
    }

    /// <inheritdoc />
    public string AskString(LocalizationKey key)
    {
        ShowMessage(key, false);

        string? stringInput;

        do
        {
            stringInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(stringInput))
            {
                ShowMessage(LocalizationKey.input_string_invalid, false);
            }
        }
        while (string.IsNullOrWhiteSpace(stringInput));

        return stringInput;
    }

    /// <inheritdoc />
    public int AskInt(LocalizationKey key)
    {
        ShowMessage(key);

        string? input;
        int numberInput;

        do
        {
            input = Console.ReadLine();

            if (!int.TryParse(input, out numberInput))
            {
                ShowMessage(LocalizationKey.input_number_invalid);
            }
        }
        while (!int.TryParse(input, out numberInput));

        return numberInput;
    }

    /// <inheritdoc />
    public BackupType AskBackupType(LocalizationKey key)
    {
        string? backupTypeInput;

        ShowMessage(LocalizationKey.backupjob_type_list);
        var values = Enum.GetValues(typeof(BackupType)).Cast<BackupType>().ToArray();
        for (int i = 0; i < values.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {values[i]}");
        }
        
        int choice;
        ShowMessage(key);
        while (true)
        {
            Console.Write("\n");
            ShowMessage(LocalizationKey.user_choice, false);
            backupTypeInput = Console.ReadLine();

            if (int.TryParse(backupTypeInput, out choice) && choice >= 1 && choice <= values.Length)
            {
                break;
            }

            ShowMessage(LocalizationKey.input_backuptype_invalid);
        }

        return values[choice - 1];
    }
    
    /// <summary>
    /// Gather a save's informations and create a BackupJob
    /// </summary>
    public void CreateBackupJob()
    {
        _menuService.DisplayLabel(LocalizationKey.menu_create);
        string nameJob = AskString(LocalizationKey.backupjob_create_name);
        string sourceJob = AskString(LocalizationKey.backupjob_create_source);
        string destinationJob = AskString(LocalizationKey.backupjob_create_destination);
        BackupType backupTypeJob = AskBackupType(LocalizationKey.backupjob_create_type);

        // send to service
        _backupAppService.CreateJob(nameJob, sourceJob, destinationJob, backupTypeJob);
        ShowMessage(LocalizationKey.backupjob_created);
        _menuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Display the informations of all BackupJob created
    /// </summary>
    public void SeeSaveList()
    {
        _menuService.DisplayLabel(LocalizationKey.menu_list);
        List<BackupJob> backupJobList = _backupAppService.GetAllJobs();
        foreach (BackupJob job in backupJobList) {
            Console.WriteLine(job.Id + " - " + job.Name);
        }
        _menuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Start a save
    /// </summary>
    public void SaveJob()
    {
        _menuService.DisplayLabel(LocalizationKey.menu_save);

        int backupIndex = AskInt(LocalizationKey.ask_backupjob_save);

        ShowMessage(LocalizationKey.backup_saving);
        _backupAppService.RunJobById(backupIndex);

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
