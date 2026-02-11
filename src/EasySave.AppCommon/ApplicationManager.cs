using EasySave.Application;
using EasySave.Backup;
using EasySave.Configuration;
using EasySave.Core;
using EasySave.Localization;
using EasySave.Log;
using EasySave.Persistence;
using EasySave.State;
using EasySave.System;
using EasySave.GUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EasySave.AppCommon
{
    /// <summary>
    /// Configures shared application services and runs the selected host.
    /// </summary>
    public class ApplicationManager
    {
        private readonly IServiceCollection _services;

        private readonly string[] _args;


        /// <summary>
        /// Initializes a new instance of <see cref="ApplicationManager"/>.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        public ApplicationManager(string[] args)
        {
            _services = new ServiceCollection();
            _args = args;
            ConfigureCommonServices();
        }

        /// <summary>
        /// Registers services shared across all hosts.
        /// </summary>
        private void ConfigureCommonServices()
        {
            // Setup configuration providers
            _services.AddSingleton<IPathProvider, DefaultPathProvider>();
            _services.AddSingleton<IJobIdProvider, SequentialJobIdProvider>();

            // Setup infrastructure
            _services.AddSingleton<ILogger>(sp =>
                CreateLogger(sp.GetRequiredService<IPathProvider>()));
            _services.AddSingleton<GlobalState>();
            _services.AddSingleton<IStateWriter, RealTimeStateWriter>();
            _services.AddSingleton<IBackupJobRepository, JsonBackupJobRepository>();
            _services.AddSingleton<IUserPreferencesRepository, JsonUserPreferencesRepository>();
            _services.AddSingleton<IFileSystem, DefaultFileSystem>();
            _services.AddSingleton<ITransferService, DefaultTransferService>();
            _services.AddSingleton<IEncryptionPolicyProvider, UserPreferencesEncryptionPolicyProvider>();
            _services.AddSingleton<IEncryptionProvider, DotNetAesEncryptionProvider>();
            _services.AddSingleton<IEncryptionProvider, ExternalEncryptionProvider>();
            _services.AddSingleton<IEncryptionProviderResolver, EncryptionProviderResolver>();
            _services.AddSingleton<IBackupEngine, BackupEngine>();
            _services.AddSingleton<ILocalizationService, LocalizationService>();
            _services.AddSingleton<ILogReader, JsonLogReader>();
            _services.AddSingleton<ILogReader, XmlLogReader>();
            _services.AddSingleton<IStateReader, JsonStateReader>();
            _services.AddSingleton<IBackupJobStateService, BackupJobStateService>();
            _services.AddSingleton<IBackupExecutionGuard, BusinessSoftwareBackupExecutionGuard>();

            // Setup application service
            _services.AddSingleton<BackupApplicationService>();
            _services.AddSingleton<ILogQueryService, LogQueryService>();

            // Setup application service
            _services.AddSingleton(sp => new BackupApplicationService(
                sp.GetRequiredService<IBackupJobRepository>(),
                sp.GetRequiredService<IBackupEngine>(),
                sp.GetRequiredService<IBackupJobStateService>(),
                sp.GetRequiredService<IBackupExecutionGuard>()));
            _services.AddSingleton<ILogQueryService, LogQueryService>();
            _services.AddSingleton<ILogNavigationService, LogNavigationService>();


            _services.AddSingleton(_args);
        }

        /// <summary>
        /// Configures and runs the provided host.
        /// </summary>
        /// <param name="host">Application host to run.</param>
        public void RunHost(IApplicationHost host)
        {
            // Host-specific configuration
            host.ConfigureServices(_services, _args);

            // Build service provider
            var serviceProvider = _services.BuildServiceProvider();

            // Run host
            host.Run(serviceProvider, _args);
        }

        /// <summary>
        /// Creates the application logger based on user preferences.
        /// </summary>
        /// <param name="pathProvider">Application path provider.</param>
        /// <returns>
        /// A configured logger instance, or a no-op logger if initialization fails.
        /// </returns>
        private static ILogger CreateLogger(IPathProvider pathProvider)
        {
            try
            {
                // Load user preferences to get log format
                var preferencesRepository = new JsonUserPreferencesRepository(pathProvider);
                var userPreferences = preferencesRepository.Load();

                // Select formatter based on user preference
                EasyLog.ILogFormatter formatter;

                if (userPreferences.LogFormat == LogFormat.Xml)
                {
                    formatter = new EasyLog.XmlLogFormatter();
                }
                else
                {
                    formatter = new EasyLog.JsonLogFormatter();
                }

                var logger = new EasyLog.DailyFileLogger(
                    formatter,
                    pathProvider,
                    EasyLogDailyFileMutexName,
                    userPreferences.LogFormat);

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
}
