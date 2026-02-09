using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using EasySave.Configuration;
using EasySave.GUI.ViewModels;
using EasySave.GUI.Views;
using EasySave.Localization;
using EasySave.Persistence;

namespace EasySave.GUI;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            // Création des services
            var pathProvider = new DefaultPathProvider();
            var preferencesRepository = new JsonUserPreferencesRepository(pathProvider);
            var localizationService = new LocalizationService();

            // Chargement des préférences sauvegardées
            var preferences = preferencesRepository.Load();
            var language = localizationService.AllCultures.ContainsKey(preferences.Language)
                ? preferences.Language
                : "fr";
            localizationService.Culture = language;
            pathProvider.SetLogDirectoryOverride(preferences.LogDirectory);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(
                    preferencesRepository,
                    localizationService,
                    pathProvider),
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
