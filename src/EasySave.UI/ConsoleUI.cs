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
    public void showMessage(string key, bool writeLine = true)
    {
        string message = LocalizationService.translateTexte(key);
        if (writeLine) Console.WriteLine(message);
        else Console.Write(message);
    }

    /// <inheritdoc />
    public void showError(string key)
    {
        Console.Error.WriteLine(key);
    }

    /// <inheritdoc />
    public string askString(string key)
    {
        showMessage(key, false);

        string? stringInput;

        do
        {
            stringInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(stringInput))
            {
                showMessage("input_string_invalid", false);
            }
        }
        while (string.IsNullOrWhiteSpace(stringInput));

        return stringInput;
    }

    /// <inheritdoc />
    public int askInt(string key)
    {
        showMessage(key);

        string? input;
        int numberInput;

        do
        {
            input = Console.ReadLine();

            if (!int.TryParse(input, out numberInput))
            {
                showMessage("input_number_invalid");
            }
        }
        while (!int.TryParse(input, out numberInput));

        return numberInput;
    }

    /// <inheritdoc />
    public BackupType askBackupType(string key)
    {
        string? backupTypeInput;
        BackupType backupType;

        showMessage("backupjob_type_list");
        var values = Enum.GetValues(typeof(BackupType)).Cast<BackupType>().ToArray();
        for (int i = 0; i < values.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {values[i]}");
        }
        
        int choice;
        showMessage(key);
        while (true)
        {
            Console.Write("\n");
            showMessage("user_choice", false);
            backupTypeInput = Console.ReadLine();

            if (int.TryParse(backupTypeInput, out choice) && choice >= 1 && choice <= values.Length)
            {
                break;
            }

            showMessage("input_backuptype_invalid");
        }

        return values[choice - 1];
    }
    
    /// <summary>
    /// Gather a save's informations and create a saveWork
    /// </summary>
    public void createSaveWork()
    {
        labelText("menu_create");
        string nameJob = askString("savework_create_name");
        string sourceJob = askString("savework_create_source");
        string destinationJob = askString("savework_create_destination");
        BackupType backupTypeJob = askBackupType("savework_create_type");

        // send to service 
        //BackupAppService.CreateJob(nameJob, sourceJob, destinationJob, backupTypeJob);
        showMessage("backupjob_created");
        waitForUser();
        mainMenu();
    }

    /// <summary>
    /// Display the informations of all saveWork created
    /// </summary>
    public void seeSaveList()
    {
        labelText("menu_list");
        List<SaveWork> saveWorkList = BackUpAppService.GetAllJobs();
        foreach (SaveWork job in saveWorkList) {
            Console.WriteLine(job.Id + " - " + job.Name);
        }
    }

    /// <summary>
    /// Start a save
    /// </summary>
    public void saveJob()
    {
        labelText("menu_save");

        int backupIndex = askInt("ask_backupjob_save");

        showMessage("backup_saving");
        BackupAppService.RunJob(backupIndex);

        waitForUser();

        mainMenu();
    }

    /// <summary>
    /// The menu of the app's configuration
    /// </summary>
    public void configureParams()
    {
        string[] menu = { "menu_params_locale", "back" };

        Dictionary<int, Action> menuActions = new()
    {
        { 0, showChangeLocale },
        { 1, mainMenu },
    };
        showMenu(menu, menuActions, "menu_params");
    }

    /// <summary>
    /// The menu to change the app language
    /// </summary>
    public void showChangeLocale()
    {
        string currentLocale = LocalizationService.getCulture();
        Dictionary<string, string> allCultures = LocalizationService.getAllCultures();
        string[] menu = allCultures
            .Values
            .Append("back")
            .ToArray();

        Dictionary<int, Action> menuActions = new()
        {
            { 0,  () => changeLocale("fr")  },
            { 1,  () => changeLocale("en")  },
            { 2, configureParams }
        };

        showMenu(menu, menuActions,"menu_params_locale");
    }

    /// <summary>
    /// The action of changing the language
    /// </summary>
    /// <param name="locale"></param>
    public void changeLocale(string locale)
    {
        LocalizationService.setCulture(locale);

        showChangeLocale();
    }

    /// <summary>
    /// The main menu of the application
    /// </summary>
    public void mainMenu()
    {
        string[] menu = { "menu_create", "menu_list", "menu_save", "menu_params", "menu_quit" };

        Dictionary<int, Action> menuActions = new()
    {
        { 0, createSaveWork },
        { 1, seeSaveList },
        { 2, saveJob },
        { 3, configureParams },
        { 4, quit } // ou juste Environment.Exit(0)
    };

        showMenu(menu, menuActions);

    }

    /// <summary>
    /// Quit the application
    /// </summary>
    public void quit()
    {
        Console.Clear();
    }

    /// <summary>
    /// Show a menu with options
    /// </summary>
    /// <param name="menu">The list of displayed text</param>
    /// <param name="menuActions">A dictionnary with the index and the actions of each options</param>
    /// <param name="menuLabel">The translation key of the name of the menu</param>
    public void showMenu(string[] menu, Dictionary<int, Action> menuActions, string menuLabel = "menu")
    {
        int index = 0;
        ConsoleKey key;

        do
        {
            Console.Clear() ;
            labelText(menuLabel);
            for (int i = 0; i < menu.Length; i++)
            {
                if (i == index)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("> ");
                    showMessage(menu[i]);
                    Console.ResetColor();
                }
                else
                {
                    Console.Write("  ");
                    showMessage(menu[i]);
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
    private void labelText(string key)
    {
        Console.Write("====");
        string message = LocalizationService.translateTexte(key);
        Console.Write(message);
        Console.Write("====\n");
    }

    private void waitForUser()
    {
        showMessage("waiting_user");
        Console.ReadKey(true);
    }
}