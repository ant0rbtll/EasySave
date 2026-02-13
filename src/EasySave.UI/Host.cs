using EasySave.Application;
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
            services.AddSingleton<ConsoleUI>();
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
