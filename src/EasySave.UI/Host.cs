using EasySave.Application;
using EasySave.Application.Services;
using EasySave.Core;
using EasySave.Localization;
using EasySave.Persistence;
using EasySave.UI.Menu;
using EasySave.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EasySave.UI
{
    /// <summary>
    /// Console host implementation.
    /// </summary>
    public class Host : IApplicationHost
    {
        /// <summary>
        /// Registers services specific to the command-line interface.
        /// </summary>
        /// <param name="services">Shared service collection.</param>
        /// <param name="args">Command-line arguments.</param>
        public void ConfigureServices(IServiceCollection services, string[] args)
        {
            services.AddSingleton<CommandLineParser>();
            services.AddSingleton<IConsoleAdapter, SystemConsoleAdapter>();
            services.AddSingleton<IMenuService, MenuService>();
            services.AddSingleton<IMenuFactory, MenuFactory>();
            services.AddSingleton<ErrorManager>();
            services.AddSingleton<IConsoleMessageService, ConsoleMessageService>();
            services.AddSingleton<IConsoleInputService, ConsoleInputService>();
            services.AddSingleton<JobEditSessionService>();
            services.AddSingleton<UserPreferences>(sp => sp.GetRequiredService<IUserPreferencesRepository>().Load());
            services.AddSingleton<JobsFlowService>();
            services.AddSingleton<SettingsFlowService>();
            services.AddSingleton<ConsoleUI>(sp =>
            {
                var settingsFlowService = sp.GetRequiredService<SettingsFlowService>();
                settingsFlowService.InitializeCulture();

                return new ConsoleUI(
                    sp.GetRequiredService<BackupApplicationService>(),
                    sp.GetRequiredService<CommandLineParser>(),
                    sp.GetRequiredService<ILocalizationService>(),
                    sp.GetRequiredService<IConsoleAdapter>(),
                    sp.GetRequiredService<IMenuService>(),
                    sp.GetRequiredService<IMenuFactory>(),
                    sp.GetRequiredService<IConsoleMessageService>(),
                    sp.GetRequiredService<IConsoleInputService>(),
                    sp.GetRequiredService<JobsFlowService>(),
                    settingsFlowService);
            });
        }

        /// <summary>
        /// Runs the console interface in interactive or direct mode based on arguments.
        /// </summary>
        /// <param name="serviceProvider">Initialized service provider.</param>
        /// <param name="args">Command-line arguments.</param>
        public void Run(IServiceProvider serviceProvider, string[] args)
        {
            var console = serviceProvider.GetRequiredService<ConsoleUI>();

            if (args.Length == 0)
            {
                console.MainMenu();
            }
            else
            {
                console.RunFromArgs(args);
            }
        }
    }
}
