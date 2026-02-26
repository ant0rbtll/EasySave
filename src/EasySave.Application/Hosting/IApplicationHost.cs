using Microsoft.Extensions.DependencyInjection;
using System;

namespace EasySave.Application
{
    /// <summary>
    /// Defines the contract for a host that can configure and run the application.
    /// </summary>
    public interface IApplicationHost
    {
        /// <summary>
        /// Registers host-specific services.
        /// </summary>
        /// <param name="services">Shared application service collection.</param>
        /// <param name="args">Command-line arguments.</param>
        void ConfigureServices(IServiceCollection services, string[] args);

        /// <summary>
        /// Runs the host using the built service container.
        /// </summary>
        /// <param name="serviceProvider">Initialized service provider.</param>
        /// <param name="args">Command-line arguments.</param>
        void Run(IServiceProvider serviceProvider, string[] args);
    }
}
