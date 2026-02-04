using System.Reflection;
using EasySave.Application;
using EasySave.Persistence;
using EasySave.Backup;
using EasySave.State;
using EasySave.Configuration;
using EasySave.System;
using EasySave.Core.Logging;


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
        var provider = InitServices();
        var console = provider.GetRequiredService<ConsoleUI>();

        if (args.Length == 0)
        {
            console.MainMenu();
        }
        else
        {
            console.RunFromArgs(args);
        }
    }

    /// <summary>
    /// Initialisation of all the services of the app
    /// </summary>
    /// <returns>The provider to get any Service</returns>
    private static IServiceProvider InitServices()
    {
        var services = new ServiceCollection();

        // Setup configuration providers
        services.AddSingleton<IPathProvider, DefaultPathProvider>();
        services.AddSingleton<IJobIdProvider, SequentialJobIdProvider>();

        // Setup infrastructure
        var logger = CreateLogger(pathProvider);
        Console.Error.WriteLine($"[EasyLog] Loaded: {logger.GetType().FullName}");
        var globalState = new GlobalState();
        var stateWriter = new RealTimeStateWriter(pathProvider, globalState);
        var repository = new JsonBackupJobRepository(pathProvider, idProvider);
        var preferencesRepository = new JsonUserPreferencesRepository(pathProvider);
        var fileSystem = new DefaultFileSystem();
        var transferService = new DefaultTransferService(fileSystem);
        var backupEngine = new BackupEngine(fileSystem, transferService, stateWriter, logger);


        // Setup application service
        services.AddSingleton<BackupAppService>();

        // Setup and run UI
        services.AddSingleton<ConsoleUI>();

        return services.BuildServiceProvider();
    }

    private static ILogger CreateLogger(IPathProvider pathProvider)
    {
        var easyLogPath = Path.Combine(AppContext.BaseDirectory, "EasyLog.dll");
        if (!File.Exists(easyLogPath))
        {
            File.WriteAllText(
                Path.Combine(AppContext.BaseDirectory, "easylog-load.txt"),
                $"EasyLog.dll not found at: {easyLogPath}");
            return new NoOpLogger();
        }

        try
        {
            var assembly = Assembly.LoadFrom(easyLogPath);

            var formatterType = assembly.GetType("EasyLog.JsonLogFormatter", throwOnError: true);
            var loggerType = assembly.GetType("EasyLog.DailyFileLogger", throwOnError: true);

            var formatter = Activator.CreateInstance(formatterType!);
            var logger = Activator.CreateInstance(
                loggerType!,
                formatter,
                pathProvider,
                "Global\\ProSoft_EasySave_EasyLog_DailyFile");

            return logger as ILogger ?? new NoOpLogger();
        }
        catch (Exception ex)
        {
            File.WriteAllText(
                Path.Combine(AppContext.BaseDirectory, "easylog-load.txt"),
                ex.ToString());
            return new NoOpLogger();
        }
    }
}
