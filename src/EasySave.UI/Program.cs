using System.Reflection;
using EasySave.Application;
using EasySave.Persistence;
using EasySave.Backup;
using EasySave.State;
using EasySave.Configuration;
using EasySave.System;
using EasySave.Log;
using Microsoft.Extensions.DependencyInjection;


namespace EasySave.UI;

/// <summary>
/// The main class that start the app
/// </summary>
public class Program
{
    // EasyLog expects this global mutex name; keep in sync with EasyLog.DailyFileLogger.
    private const string EasyLogDailyFileMutexName = "Global\\ProSoft_EasySave_EasyLog_DailyFile";

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
        services.AddSingleton<ILogger>(sp =>
            CreateLogger(sp.GetRequiredService<IPathProvider>()));
        services.AddSingleton<GlobalState>();
        services.AddSingleton<IStateWriter, RealTimeStateWriter>();
        services.AddSingleton<IBackupJobRepository, JsonBackupJobRepository>();
        services.AddSingleton<IUserPreferencesRepository, JsonUserPreferencesRepository>();
        services.AddSingleton<IFileSystem, DefaultFileSystem>();
        services.AddSingleton<ITransferService, DefaultTransferService>();
        services.AddSingleton<BackupEngine>();
        services.AddSingleton<CommandLineParser>();

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
            return new NoOpLogger();

        try
        {
            var assembly = Assembly.LoadFrom(easyLogPath);

            // Expected EasyLog types (string-based reflection is brittle; keep names in sync).
            var formatterType = assembly.GetType("EasyLog.JsonLogFormatter", throwOnError: false);
            var loggerType = assembly.GetType("EasyLog.DailyFileLogger", throwOnError: false);
            if (formatterType is null || loggerType is null)
                return new NoOpLogger();

            var formatter = Activator.CreateInstance(formatterType!);
            var logger = Activator.CreateInstance(
                loggerType!,
                formatter,
                pathProvider,
                EasyLogDailyFileMutexName);

            return logger as ILogger ?? new NoOpLogger();
        }
        catch (Exception ex)
        {
            try
            {
                Console.Error.WriteLine($"EasyLog initialization failed: {ex}");
            }
            catch
            {
                // Best-effort logging only.
            }

            return new NoOpLogger();
        }
    }
}
