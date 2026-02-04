using EasySave.Application;
using EasySave.Persistence;
using EasySave.Backup;
using EasySave.State;
using EasySave.Configuration;
using EasySave.System;
using EasyLog;

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
        // Setup configuration providers
        var pathProvider = new DefaultPathProvider();
        var idProvider = new SequentialJobIdProvider();
        
        // Setup infrastructure
        var formatter = new JsonLogFormatter();
        var logger = new DailyFileLogger(formatter, pathProvider);
        var globalState = new GlobalState();
        var stateWriter = new RealTimeStateWriter(pathProvider, globalState);
        var repository = new JsonBackupJobRepository(pathProvider, idProvider);
        var preferencesRepository = new JsonUserPreferencesRepository(pathProvider);
        var fileSystem = new DefaultFileSystem();
        var transferService = new DefaultTransferService(fileSystem);
        var backupEngine = new BackupEngine(fileSystem, transferService, stateWriter, logger);

        // Setup application service
        var backupAppService = new BackupAppService(repository, backupEngine);

        // Setup and run UI
        var console = new ConsoleUI(backupAppService, preferencesRepository);
        console.MainMenu();
    }
}
