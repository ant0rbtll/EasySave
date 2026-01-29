using EasySave.Application;
using EasySave.Localization;

namespace EasySave.UI;

public class ConsoleUI : IUI
{

    private BackupAppService backUpAppService = new BackupAppService();
    private ILocalizationService localizationService = new LocalizationService();
    
    public void showMessage(string key)
    {
        string message = localizationService.translateTexte(key);
        Console.WriteLine(message);
    }

    public void showError(string key)
    {
        Console.Error.WriteLine(key);
    }

    public string askString(string key)
    {
        showMessage(key);
        string response = Console.ReadLine();
        return response;
    }

    public int askInt(string key)
    {
        showMessage(key);
        int response = Convert.ToInt16(Console.ReadLine());

        //verif if number
        return response;
    }

    public string askBackupType(string key)
    {
        showMessage(key);
        string response = Console.ReadLine();
        return response;
    }

    public void createSaveWork()
    {
        labelText("menu_create");
        string nameJob = askString("savework_create_name");
        string sourceJob = askString("savework_create_source");
        string destination = askString("savework_create_destination");
        string backupTypeJob = askBackupType("savework_create_type");
        // send to service 
        showMenu();
    }

    public void seeJobsList()
    {
        labelText("menu_list");
        List<string> listJobs = ["job", "job 2"];
        foreach (string job in listJobs) {
            showMessage(job);
        }
        //showMenu();

    }

    public void saveJob()
    {
        labelText("menu_save");
        seeJobsList();


        showMenu();
    }

    public void configureParams()
    {
        labelText("menu_params");


    }


    public void showMenu()
    {
        separator();
        string[] menu = { "menu_create", "menu_list", "menu_save", "menu_params" ,"menu_quit" };
        int index = 0;
        ConsoleKey key;

        do
        {
            Console.Clear();
            labelText("menu");
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
                index--;
            else if (key == ConsoleKey.DownArrow && index < menu.Length - 1)
                index++;

        } while (key != ConsoleKey.Enter);

        Console.Clear();
        if (index == 0)
        {
            createSaveWork();
        }
        else if (index == 1)
        {
            seeJobsList();
        }
        else if (index == 2)
        {
            saveJob();
        }
        else if (index == 3)
        {
            configureParams();
        }
    }

    private void separator()
    {
        Console.WriteLine("====================================================");
    }

    private void labelText(string key)
    {
        Console.Write("====");
        string message = localizationService.translateTexte(key);
        Console.Write(message);
        Console.Write("====\n");
    }
}

public class MainClass
{
    public static void Main(string[] args)
    {
        ConsoleUI console = new ConsoleUI();
        console.showMenu();

    }
}
 