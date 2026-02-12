using Avalonia;
using EasySave.Application;
using EasySave.Backup;
using EasySave.Configuration;
using EasySave.GUI.ViewModels;
using EasySave.Localization;
using EasySave.Log;
using EasySave.Persistence;
using EasySave.State;
using EasySave.System;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EasySave.GUI;

sealed class Program
{

    private const string EasyLogDailyFileMutexName = "Global\\ProSoft_EasySave_EasyLog_DailyFile";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Console.Error.WriteLine($"[EasySave] Unhandled exception: {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            // Ignore known harmless Avalonia/DBus errors on Linux
            if (e.Exception.InnerException is not null
                && e.Exception.InnerException.GetType().Name == "DBusException")
            {
                e.SetObserved();
                return;
            }

            Console.Error.WriteLine($"[EasySave] Unobserved task exception: {e.Exception}");
            e.SetObserved();
        };

        // Initialiser les services AVANT de démarrer Avalonia
        var services = InitServices();

        BuildAvaloniaApp(services)
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IServiceProvider? services = null)
        => AppBuilder.Configure(() => new App(services ?? InitServices()))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

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
        services.AddSingleton<IEncryptionPolicyProvider, UserPreferencesEncryptionPolicyProvider>();
        services.AddSingleton<IEncryptionProvider, DotNetAesEncryptionProvider>();
        services.AddSingleton<IEncryptionProvider, ExternalEncryptionProvider>();
        services.AddSingleton<IEncryptionProviderResolver, EncryptionProviderResolver>();
        services.AddSingleton<IBackupExecutionGuard, BusinessSoftwareBackupExecutionGuard>();
        services.AddSingleton<IBackupEngine, BackupEngine>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<ILogReader, JsonLogReader>();
        services.AddSingleton<ILogReader, XmlLogReader>();
        services.AddSingleton<IStateReader, JsonStateReader>();
        services.AddSingleton<IBackupJobStateService, BackupJobStateService>();

        // Setup application service
        services.AddSingleton(sp => new BackupApplicationService(
            sp.GetRequiredService<IBackupJobRepository>(),
            sp.GetRequiredService<IBackupEngine>(),
            sp.GetRequiredService<IBackupJobStateService>(),
            sp.GetRequiredService<IBackupExecutionGuard>()));
        services.AddSingleton<ILogQueryService, LogQueryService>();

        // Setup ViewModels pour Avalonia
        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<CreateViewModel>();
        services.AddSingleton<ManageViewModel>();
        services.AddSingleton<LogViewModel>();
        services.AddSingleton<ConfigViewModel>();
        services.AddSingleton<SidebarViewModel>();

        return services.BuildServiceProvider();
    }

    private static ILogger CreateLogger(IPathProvider pathProvider)
    {
        try
        {
            var formatter = new EasyLog.JsonLogFormatter();
            var logger = new EasyLog.DailyFileLogger(
                formatter,
                pathProvider,
                EasyLogDailyFileMutexName);
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
