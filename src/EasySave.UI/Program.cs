using EasySave.Application;
using EasySave.Persistence;
using EasySave.Backup;
using EasySave.State;
using EasySave.Configuration;
using EasySave.System;
using EasyLog;
using Microsoft.Extensions.DependencyInjection;


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
        var provider = initServices();
        var console = provider.GetRequiredService<ConsoleUI>();

        if (args.Length == 0)
        {
            console.MainMenu();
        } else
        {
            console.RunFromArgs(CommandLineParser.Parse(args));
        }
    }

    private static IServiceProvider initServices()
    {
        var services = new ServiceCollection();

        // Setup configuration providers
        services.AddSingleton<IPathProvider, DefaultPathProvider>();
        services.AddSingleton<IJobIdProvider, SequentialJobIdProvider>();

        // Setup infrastructure
        services.AddSingleton<ILogFormatter, JsonLogFormatter>();
        services.AddSingleton<ILogger, DailyFileLogger>();
        services.AddSingleton<GlobalState>();
        services.AddSingleton<IStateWriter, RealTimeStateWriter>();
        services.AddSingleton<IBackupJobRepository, JsonBackupJobRepository>();
        services.AddSingleton<IUserPreferencesRepository, JsonUserPreferencesRepository>();
        services.AddSingleton<IFileSystem, DefaultFileSystem>();
        services.AddSingleton<ITransferService, DefaultTransferService>();
        services.AddSingleton<BackupEngine>();

        // Setup application service
        services.AddSingleton<BackupAppService>();

        // Setup and run UI
        services.AddSingleton<ConsoleUI>();

        return services.BuildServiceProvider();
    }
}
