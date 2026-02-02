using EasySave.Application;

namespace EasySave.UI;

/// <summary>
/// The main class that start the app
/// </summary>
public class Program
{
    /// <summary>
    /// The main action that start the app
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void Main(string[] args)
    {
        BackupAppService backupAppService = new BackupAppService();

        ConsoleUI console = new ConsoleUI(backupAppService);
        console.MainMenu();

    }
}
