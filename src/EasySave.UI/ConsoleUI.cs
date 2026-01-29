using EasySave.Application;
using EasySave.Localization;

namespace EasySave.UI;

/// <summary>
/// Actions and display of the application with the console
/// </summary>
public class ConsoleUI : IUI
{

    private BackupAppService backUpAppService = new BackupAppService();
    private ILocalizationService localizationService = new LocalizationService();
    
    /// <inheritdoc />
    public void showMessage(string key)
    {
        string message = localizationService.translateTexte(key);
        Console.WriteLine(message);
    }

    /// <inheritdoc />
    public void showError(string key)
    {
        Console.Error.WriteLine(key);
    }

    /// <inheritdoc />
    public string askString(string key)
    {
        showMessage(key);

        string? stringInput;

        do
        {
            stringInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(stringInput))
            {
                showMessage("input_string_invalid");
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
    public string askBackupType(string key)
    {
        return askString(key);
        /* in waiting of the list of backupTypes
        showMessage(key);
        string? response = Console.ReadLine();
        return response;
        */
    }
    
    /// <summary>
    /// Gather a save's informations and create a saveWork
    /// </summary>
    public void createSaveWork()
    {
        labelText("menu_create");
        string nameJob = askString("savework_create_name");
        string sourceJob = askString("savework_create_source");
        string destination = askString("savework_create_destination");
        string backupTypeJob = askBackupType("savework_create_type");
        // send to service 
        mainMenu();
    }

    /// <summary>
    /// Display the informations of all saveWork created
    /// </summary>
    public void seeSaveList()
    {
        labelText("menu_list");
        List<string> listJobs = ["job", "job 2"];
        foreach (string job in listJobs) {
            showMessage(job);
        }
        //showMenu();

    }

    /// <summary>
    /// Start a save
    /// </summary>
    public void saveJob()
    {
        labelText("menu_save");
        seeSaveList();

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
        string currentLocale = localizationService.getCulture();
        Dictionary<string, string> allCultures = localizationService.getAllCultures();
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
        localizationService.setCulture(locale);

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
        string message = localizationService.translateTexte(key);
        Console.Write(message);
        Console.Write("====\n");
    }
}

/// <summary>
/// The main class that start the app
/// </summary>
public class MainClass
{
    /// <summary>
    /// The main action that start the app
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        ConsoleUI console = new ConsoleUI();
        console.mainMenu();

    }
}
 