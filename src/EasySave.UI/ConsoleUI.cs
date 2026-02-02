using EasySave.Application;
using EasySave.Localization;
using EasySave.Core;

namespace EasySave.UI;

/// <summary>
/// Actions and display of the application with the console
/// </summary>
public class ConsoleUI : IUI
{

    private BackupAppService BackUpAppService;
    private ILocalizationService LocalizationService = new LocalizationService();
    
    public ConsoleUI(BackupAppService backUpAppService)
    {
        BackUpAppService = backUpAppService;
    }

    /// <inheritdoc />
    public void ShowMessage(string key, bool writeLine = true)
    {
        string message = LocalizationService.TranslateTexte(key);
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
    /// Gather a save's informations and create a saveWork
    /// </summary>
    public void CreateSaveWork()
    {
        LabelText("menu_create");
        string nameJob = AskString("savework_create_name");
        string sourceJob = AskString("savework_create_source");
        string destinationJob = AskString("savework_create_destination");
        BackupType backupTypeJob = AskBackupType("savework_create_type");

        // send to service 
        //BackupAppService.CreateJob(nameJob, sourceJob, destinationJob, backupTypeJob);
        ShowMessage("backupjob_created");
        WaitForUser();
        MainMenu();
    }

    /// <summary>
    /// Display the informations of all saveWork created
    /// </summary>
    public void SeeSaveList()
    {
        LabelText("menu_list");
        List<SaveWork> saveWorkList = BackUpAppService.GetAllJobs();
        foreach (SaveWork job in saveWorkList) {
            Console.WriteLine(job.Id + " - " + job.Name);
        }
    }

    /// <summary>
    /// Start a save
    /// </summary>
    public void SaveJob()
    {
        LabelText("menu_save");

        int backupIndex = AskInt("ask_backupjob_save");

        ShowMessage("backup_saving");
        BackupAppService.RunJob(backupIndex);

        WaitForUser();

        MainMenu();
    }

    /// <summary>
    /// The menu of the app's configuration
    /// </summary>
    public void ConfigureParams()
    {
        string[] menu = { "menu_params_locale", "back" };

        Dictionary<int, Action> menuActions = new()
    {
        { 0, ShowChangeLocale },
        { 1, MainMenu },
    };
        ShowMenu(menu, menuActions, "menu_params");
    }

    /// <summary>
    /// The menu to change the app language
    /// </summary>
    public void ShowChangeLocale()
    {
        string currentLocale = LocalizationService.getCulture();
        Dictionary<string, string> allCultures = LocalizationService.getAllCultures();
        string[] menu = allCultures
            .Values
            .Append("back")
            .ToArray();

        Dictionary<int, Action> menuActions = new()
        {
            { 0,  () => ChangeLocale("fr")  },
            { 1,  () => ChangeLocale("en")  },
            { 2, ConfigureParams }
        };

        ShowMenu(menu, menuActions,"menu_params_locale");
    }

    /// <summary>
    /// The action of changing the language
    /// </summary>
    /// <param name="locale"></param>
    public void ChangeLocale(string locale)
    {
        LocalizationService.setCulture(locale);

        ShowChangeLocale();
    }

    /// <summary>
    /// The main menu of the application
    /// </summary>
    public void MainMenu()
    {
        string[] menu = { "menu_create", "menu_list", "menu_save", "menu_params", "menu_quit" };

        Dictionary<int, Action> menuActions = new()
    {
        { 0, CreateSaveWork },
        { 1, SeeSaveList },
        { 2, SaveJob },
        { 3, ConfigureParams },
        { 4, Quit } // ou juste Environment.Exit(0)
    };

        ShowMenu(menu, menuActions);

    }

    /// <summary>
    /// Quit the application
    /// </summary>
    public void Quit()
    {
        Console.Clear();
    }

    /// <summary>
    /// Show a menu with options
    /// </summary>
    /// <param name="menu">The list of displayed text</param>
    /// <param name="menuActions">A dictionnary with the index and the actions of each options</param>
    /// <param name="menuLabel">The translation key of the name of the menu</param>
    public void ShowMenu(string[] menu, Dictionary<int, Action> menuActions, string menuLabel = "menu")
    {
        int index = 0;
        ConsoleKey key;

        do
        {
            Console.Clear() ;
            LabelText(menuLabel);
            for (int i = 0; i < menu.Length; i++)
            {
                if (i == index)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("> ");
                    ShowMessage(menu[i]);
                    Console.ResetColor();
                }
                else
                {
                    Console.Write("  ");
                    ShowMessage(menu[i]);
                }
            }

            key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.UpArrow && index > 0)
            {
                index--;
                Console.Clear();

            }
            else if (key == ConsoleKey.DownArrow && index < menu.Length - 1)
            {
                index++;
                Console.Clear();

            }

        } while (key != ConsoleKey.Enter);
        Console.Clear();

        if (menuActions.TryGetValue(index, out var action))
        {
            action();
        }
    }

    /// <summary>
    /// Display the title of the action selected
    /// </summary>
    /// <param name="key">the translation key</param>
    private void LabelText(string key)
    {
        Console.Write("====");
        string message = LocalizationService.TranslateTexte(key);
        Console.Write(message);
        Console.Write("====\n");
    }

    /// <summary>
    /// Waiting for the user to enter any key
    /// </summary>
    private void WaitForUser()
    {
        ShowMessage("waiting_user");
        Console.ReadKey(true);
    }
}