using EasySave.Application;

namespace EasySave.UI;

public class ConsoleUI : IUI
{

    private BackupAppService backUpAppService = new BackupAppService();
    
    public void showMessage(string key)
    {
        Console.WriteLine(key);
    }

    public void showError(string key)
    {
        Console.Error.WriteLine(key);
    }

    public string askString(string key)
    {
        Console.WriteLine(key);
        string response = Console.ReadLine();
        return response;
    }

    public int askInt(string key)
    {
        Console.WriteLine(key);
        int response = Convert.ToInt16(Console.ReadLine());

        //verif if number
        return response;
    }

    public string askBackupType(string key)
    {
        Console.WriteLine(key);
        string response = Console.ReadLine();
        return response;
    }

    public void createSaveWork()
    {
        write("====Création d'un job de travail====");
        string nameJob = askString("name");
        string sourceJob = askString("source");
        string destination = askString("dest");
        string backupTypeJob = askBackupType("type");
        // send to service 
        showMenu();
    }

    public void seeJobsList()
    {
        write("====Voir la liste des jobs====");
        List<string> listJobs = ["job", "job 2"];
        foreach (string job in listJobs) {
            Console.WriteLine(job);
        }
        //showMenu();

    }

    public void saveJob()
    {
        write("====Lancer une sauvegarde====");
        seeJobsList();


        showMenu();
    }

    public void configureParams()
    {
        write("====Changer les paramètres====");


    }

    public void write(string message)
    {
        Console.WriteLine(message);
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
                    Console.WriteLine("> " + menu[i]);
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("  " + menu[i]);
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
        Console.WriteLine("Bonjour");
        ConsoleUI console = new ConsoleUI();
        console.showMenu();

    }
}
 