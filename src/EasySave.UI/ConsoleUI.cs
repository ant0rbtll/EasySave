using EasySave.Application;
using EasySave.Localization;
using EasySave.Core;
using EasySave.UI.Menu;

namespace EasySave.UI;

/// <summary>
/// Actions and display of the application with the console
/// </summary>
public class ConsoleUI : IUI
{

    private readonly BackupAppService BackUpAppService;
    public ILocalizationService LocalizationService { get; }
    private readonly MenuService MenuService;
    private readonly MenuFactory MenuFactory;

    public ConsoleUI(BackupAppService backUpAppService)
    {
        BackUpAppService = backUpAppService;
        LocalizationService = new LocalizationService();
        MenuService = new MenuService(LocalizationService);
        MenuFactory = new MenuFactory(this);
    }

    /// <inheritdoc />
    public void ShowMessage(string key, bool writeLine = true)
    {
        string message = LocalizationService.TranslateText(key);
        if (writeLine) Console.WriteLine(message);
        else Console.Write(message);
    }

    /// <inheritdoc />
    public void ShowError(string key)
    {
        Console.Error.WriteLine(key);
    }

    /// <inheritdoc />
    public string AskString(string key)
    {
        ShowMessage(key, false);

        string? stringInput;

        do
        {
            stringInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(stringInput))
            {
                ShowMessage("input_string_invalid", false);
            }
        }
        while (string.IsNullOrWhiteSpace(stringInput));

        return stringInput;
    }

    /// <inheritdoc />
    public int AskInt(string key)
    {
        ShowMessage(key);

        string? input;
        int numberInput;

        do
        {
            input = Console.ReadLine();

            if (!int.TryParse(input, out numberInput))
            {
                ShowMessage("input_number_invalid");
            }
        }
        while (!int.TryParse(input, out numberInput));

        return numberInput;
    }

    /// <inheritdoc />
    public BackupType AskBackupType(string key)
    {
        string? backupTypeInput;
        BackupType backupType;

        ShowMessage("backupjob_type_list");
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
            ShowMessage("user_choice", false);
            backupTypeInput = Console.ReadLine();

            if (int.TryParse(backupTypeInput, out choice) && choice >= 1 && choice <= values.Length)
            {
                break;
            }

            ShowMessage("input_backuptype_invalid");
        }

        return values[choice - 1];
    }
    
    /// <summary>
    /// Gather a save's informations and create a BackupJob
    /// </summary>
    public void CreateBackupJob()
    {
        MenuService.DisplayLabel("menu_create");
        string nameJob = AskString("backupjob_create_name");
        string sourceJob = AskString("backupjob_create_source");
        string destinationJob = AskString("backupjob_create_destination");
        BackupType backupTypeJob = AskBackupType("backupjob_create_type");

        // send to service 
        BackupAppService.CreateJob(nameJob, sourceJob, destinationJob, backupTypeJob);
        ShowMessage("backupjob_created");
        MenuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Display the informations of all BackupJob created
    /// </summary>
    public void SeeSaveList()
    {
        MenuService.DisplayLabel("menu_list");
        List<BackupJob> backupJobList = BackUpAppService.GetAllJobs();
        foreach (BackupJob job in backupJobList) {
            Console.WriteLine(job.Id + " - " + job.Name);
        }
        MenuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Start a save
    /// </summary>
    public void SaveJob()
    {
        MenuService.DisplayLabel("menu_save");

        int backupIndex = AskInt("ask_backupjob_save");

        ShowMessage("backup_saving");
        BackupAppService.RunJob(backupIndex);

        MenuService.WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// The menu of the app's configuration
    /// </summary>
    public void ConfigureParams()
    {
        var menuConfig = MenuFactory.CreateParamsMenu();
        MenuService.ShowMenuWithActions(menuConfig.Items, menuConfig.Actions, menuConfig.Label);
    }

    /// <summary>
    /// The menu to change the app language
    /// </summary>
    public void ShowChangeLocale()
    {
        var menuConfig = MenuFactory.CreateLocaleMenu();
        MenuService.ShowMenuWithActions(menuConfig.Items, menuConfig.Actions, menuConfig.Label);
    }

    /// <summary>
    /// The action of changing the language
    /// </summary>
    /// <param name="locale"></param>
    public void ChangeLocale(string locale)
    {
        LocalizationService.Culture = locale;

        ShowChangeLocale();
    }

    /// <summary>
    /// The main menu of the application
    /// </summary>
    public void MainMenu()
    {
        var menuConfig = MenuFactory.CreateMainMenu();
        MenuService.ShowMenuWithActions(menuConfig.Items, menuConfig.Actions, menuConfig.Label);

    }

    /// <summary>
    /// Quit the application
    /// </summary>
    public void Quit()
    {
        Console.Clear();
    }
}