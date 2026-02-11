using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Application
{
    public interface IApplicationHost
    {
        string Name { get; }
        void ConfigureServices(IServiceCollection services, string[] args);
        void Run(IServiceProvider serviceProvider, string[] args);
    }
}
