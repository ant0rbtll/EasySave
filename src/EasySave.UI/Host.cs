using EasySave.Application;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EasySave.UI
{
    public class Host : IApplicationHost
    {
        public void ConfigureServices(IServiceCollection services, string[] args)
        {
            services.AddSingleton<CommandLineParser>();
            services.AddSingleton<ConsoleUI>();
        }

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
