using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using EasySave.Configuration;
using EasySave.GUI.ViewModels;
using EasySave.GUI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace EasySave.GUI;

public partial class App : Avalonia.Application
{

    private readonly IServiceProvider _services;

    // Constructeur par d�faut pour le designer
    public App() : this(null!)
    {
    }

    // Constructeur avec injection de d�pendances
    public App(IServiceProvider services)
    {
        _services = services;
    }


    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Acc�s global au ServiceProvider pour r�solution de d�pendances
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            // Cr�ation des services
            var pathProvider = _services.GetRequiredService<IPathProvider>();
            var preferencesRepository = _services.GetRequiredService<IUserPreferencesRepository>(),;
            var localizationService = _services.GetRequiredService<ILocalizationService>();

            // Chargement des pr�f�rences sauvegard�es
            var preferences = preferencesRepository.Load();
            var language = localizationService.AllCultures.ContainsKey(preferences.Language)
                ? preferences.Language
                : "fr";
            localizationService.Culture = language;
            pathProvider.SetLogDirectoryOverride(preferences.LogDirectory);

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
