using EasySave.Application;
using EasySave.Core;
using EasyLog;
using EasySave.Backup;
using EasySave.Persistence;
using EasySave.Configuration;
using EasySave.System;
using EasySave.State;

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
        Console.Clear();

        var eventService = new EventService();
        var console = new ConsoleUI(eventService);

        var provider = new SequentialJobIdProvider();

        var repo = new InMemoryBackupJobRepository(provider);


        var fileSystem = new DefaultFileSystem();
        var transferService = new DefaultTransferService(fileSystem);
        var pathProvider = new DefaultPathProvider();

        var globalState = new GlobalState();
        var stateWritter = new RealTimeStateWriter(pathProvider, globalState);

        var formatter = new JsonLogFormatter();
        var logger = new DailyFileLogger(formatter,pathProvider);


        var engine = new BackupEngine(fileSystem, transferService, stateWritter, logger);

        var app = new BackupAppService(repo, engine, eventService);

        app.RunInteractive();

        //var backupAppService = this.init();

        console.MainMenu();

    }

    /*public static void init()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IJobIdProvider, SequentialJobIdProvider>();
        services.AddSingleton<IBackupJobRepository, InMemoryBackupJobRepository>();
        services.AddSingleton<IEventService, EventService>();
        services.AddSingleton<IBackupEngine, BackUpEngine>();
        services.AddSingleton<BackupAppService>();

        var provider = services.BuildServiceProvider();

        var backupAppService = provider.GetRequiredService<BackupAppService>();
        backupAppService.Run();
    }*/
}
