using Microsoft.Extensions.DependencyInjection;
using System;

namespace EasySave.Application
{
    public interface IApplicationHost
    {
        void ConfigureServices(IServiceCollection services, string[] args);
        void Run(IServiceProvider serviceProvider, string[] args);
    }
}
