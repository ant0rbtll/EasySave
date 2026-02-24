using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using EasySave.Application;
using EasySave.Configuration;
using EasySave.GUI.Services;
using EasySave.GUI.ViewModels;
using EasySave.GUI.Views;
using EasySave.Localization;
using EasySave.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace EasySave.GUI;

public partial class App : Avalonia.Application
{

    private readonly IServiceProvider? _services;

    // Constructeur par défaut pour le designer
    public App()
    {
    }

    // Constructeur avec injection de dépendances
    public App(IServiceProvider services)
    {
        _services = services;
    }


    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && _services is not null)
        {
            DisableAvaloniaDataAnnotationValidation();

            // Création des services
            var pathProvider = _services.GetRequiredService<IPathProvider>();
            var preferencesRepository = _services.GetRequiredService<IUserPreferencesRepository>();
            var localizationService = _services.GetRequiredService<ILocalizationService>();
            var backupApplicationService = _services.GetRequiredService<BackupApplicationService>();

            // Chargement des préférences sauvegardées
            var preferences = preferencesRepository.Load();
            var language = localizationService.AllCultures.ContainsKey(preferences.Language)
                ? preferences.Language
                : "fr";
            localizationService.Culture = language;
            pathProvider.SetLogDirectoryOverride(preferences.LogDirectory);

            var themeService = _services.GetRequiredService<ThemeService>();
            themeService.ApplyTheme(preferences.ThemePreference);

            backupApplicationService.ReconcileStartupState();

            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
