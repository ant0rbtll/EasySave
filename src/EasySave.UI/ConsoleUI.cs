namespace EasySave.UI;

public class ConsoleUI : IUI
{
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

    public static Main(string[] args)
    {
        Console.WriteLine('Bonjour');
    }
}