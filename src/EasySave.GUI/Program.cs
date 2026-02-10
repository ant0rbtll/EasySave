using Avalonia;
using EasySave.Application;
using EasySave.Backup;
using EasySave.Configuration;
using EasySave.GUI.ViewModels;
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
        services.AddSingleton<IBackupEngine, BackupEngine>();

        // Setup application service
        services.AddSingleton<BackupApplicationService>();

        // Setup ViewModels pour Avalonia
        services.AddSingleton<MainWindowViewModel>();

        services.AddTransient<CreateViewModel>();
        services.AddTransient<ManageViewModel>();
        services.AddTransient<ProgressViewModel>();
        services.AddTransient<LogViewModel>();
        services.AddTransient<ConfigViewModel>();
        services.AddTransient<SidebarViewModel>();

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
