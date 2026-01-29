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
        showMessage("====Création d'un job de travail====");
        string nameJob = askString("name");
        string sourceJob = askString("source");
        string destination = askString("dest");
        string backupTypeJob = askBackupType("type");
        // send to service 
        showMenu();
    }

    public void seeJobsList()
    {
        showMessage("====Voir la liste des jobs====");
        List<string> listJobs = ["job", "job 2"];
        foreach (string job in listJobs) {
            showMessage(job);
        }
        //showMenu();

    }

    public void saveJob()
    {
        showMessage("====Lancer une sauvegarde====");
        seeJobsList();


        showMenu();
    }

    public void configureParams()
    {
        showMessage("====Changer les paramètres====");


    }


    public void showMenu()
    {
        separator();
        string[] menu = { "Création d'un job de travail", "Voir la liste des jobs", "Lancer une sauvegarde", "Changer les paramètres" ,"Quitter" };
        int index = 0;
        ConsoleKey key;

        do
        {
            Console.Clear();
            Console.WriteLine("====Menu====\n");
            for (int i = 0; i < menu.Length; i++)
            {
                if (i == index)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    showMessage("> " + menu[i]);
                    Console.ResetColor();
                }
                else
                {
                    showMessage("  " + menu[i]);
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
}

public class MainClass
{
    public static void Main(string[] args)
    {
        ConsoleUI console = new ConsoleUI();
        console.showMenu();

    }
}
 