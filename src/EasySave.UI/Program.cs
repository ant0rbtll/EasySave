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
/// Application entry point that configures dependency injection and starts the UI.
/// </summary>
public class Program
{
    // EasyLog expects this global mutex name; keep in sync with EasyLog.DailyFileLogger.
    private const string EasyLogDailyFileMutexName = "Global\\ProSoft_EasySave_EasyLog_DailyFile";

    /// <summary>
    /// Application entry point.
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
    /// Registers and configures all application services.
    /// </summary>
    /// <returns>The configured service provider.</returns>
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
        services.AddSingleton<IBackupEngine, BackupEngine>();
        services.AddSingleton<CommandLineParser>();

        // Setup application service
        services.AddSingleton<BackupApplicationService>();

        // Setup and run UI
        services.AddSingleton<ConsoleUI>();

        return services.BuildServiceProvider();
    }

    private static ILogger CreateLogger(IPathProvider pathProvider)
    {
        try
        {
            // Load user preferences to get log format
            var preferencesRepository = new JsonUserPreferencesRepository(pathProvider);
            var userPreferences = preferencesRepository.Load();

            // Select formatter and extension based on user preference
            EasyLog.ILogFormatter formatter;
            string fileExtension;

            if (userPreferences.LogFormat == EasyLog.LogFormat.Xml)
            {
                formatter = new EasyLog.XmlLogFormatter();
                fileExtension = "xml";
            }
            else
            {
                formatter = new EasyLog.JsonLogFormatter();
                fileExtension = "json";
            }

            var logger = new EasyLog.DailyFileLogger(
                formatter,
                pathProvider,
                EasyLogDailyFileMutexName,
                fileExtension);

            return logger;
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
