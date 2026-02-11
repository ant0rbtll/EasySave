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
    public class ApplicationManager
    {
        private readonly IServiceCollection _services;

        private readonly string[] _args;


        public ApplicationManager(string[] args)
        {
            _services = new ServiceCollection();
            _args = args;
            ConfigureCommonServices();
        }

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

        public void RunHost(IApplicationHost host)
        {
            // Configuration spécifique du host
            host.ConfigureServices(_services, _args);

            // Build service provider
            var serviceProvider = _services.BuildServiceProvider();

            // Lancement du host
            host.Run(serviceProvider, _args);
        }
    }
}
