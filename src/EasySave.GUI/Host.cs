using Avalonia;
using EasySave.Application;
using EasySave.GUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EasySave.GUI
{
    /// <summary>
    /// Avalonia-based implementation of the GUI host.
    /// </summary>
    public class Host : IApplicationHost
    {
        /// <summary>
        /// Registers services and ViewModels specific to the graphical interface.
        /// </summary>
        /// <param name="services">Shared service collection.</param>
        /// <param name="args">Command-line arguments.</param>
        public void ConfigureServices(IServiceCollection services, string[] args)
        {
            // GUI-specific ViewModels
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<CreateViewModel>();
            services.AddSingleton<ManageViewModel>();
            services.AddSingleton<ProgressViewModel>();
            services.AddSingleton<LogViewModel>();
            services.AddSingleton<ConfigViewModel>();
            services.AddSingleton<SidebarViewModel>();
        }

        /// <summary>
        /// Starts the GUI application and configures global exception handling.
        /// </summary>
        /// <param name="serviceProvider">Initialized service provider.</param>
        /// <param name="args">Command-line arguments.</param>
        public void Run(IServiceProvider serviceProvider, string[] args)
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

            // Initialize services BEFORE starting Avalonia

            BuildAvaloniaApp(serviceProvider)
                .StartWithClassicDesktopLifetime(args);
        }

        /// <summary>
        /// Builds the Avalonia application configuration.
        /// </summary>
        /// <param name="services">Service provider injected into the Avalonia app.</param>
        /// <returns>An Avalonia app builder ready to be started.</returns>
        public static AppBuilder BuildAvaloniaApp(IServiceProvider services)
            => AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
