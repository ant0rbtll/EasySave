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

    public static void Main(string[] args)
    {
<<<<<<< HEAD
        Console.WriteLine("Bonjour");
    }

    public string showMenu()
    {
        this.separator();
        string[] menu = { "Jouer", "Options", "Quitter" };
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
        Console.WriteLine($"Tu as choisi : {menu[index]}");

    }

    private void separator()
    {
        Console.WriteLine("====================================================");
=======
        // show the choice to select language
        Console.WriteLine("Bonjour");




>>>>>>> f36e3227a4d513cc649bab9bcdfa7fb9084c2bed
    }
}