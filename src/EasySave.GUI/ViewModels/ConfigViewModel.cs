using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Configuration;
using EasySave.Localization;
using EasySave.Persistence;

namespace EasySave.GUI.ViewModels;

public partial class ConfigViewModel : ViewModelBase
{
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IPathProvider _pathProvider;
    private Action _onLanguageChanged;

    /// <summary>
    /// Fonction fournie par la couche UI pour ouvrir le sélecteur de dossier natif.
    /// Retourne le chemin sélectionné ou null si annulé.
    /// </summary>
    public Func<Task<string?>>? BrowseFolder { get; set; }

    [ObservableProperty] private string selectedLanguage;
    [ObservableProperty] private string? logDirectory;
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private string? pathError;

    // Propriétés traduites
    [ObservableProperty] private string titleText = "";
    [ObservableProperty] private string languageText = "";
    [ObservableProperty] private string logDirectoryText = "";
    [ObservableProperty] private string logDirectoryWatermark = "";
    [ObservableProperty] private string resetText = "";
    [ObservableProperty] private string saveText = "";
    [ObservableProperty] private string browseText = "";

    public ObservableCollection<LanguageOption> AvailableLanguages { get; } =
    [
        new LanguageOption("fr", "Français"),
        new LanguageOption("en", "English"),
    ];

    public ConfigViewModel(
        IUserPreferencesRepository preferencesRepository,
        ILocalizationService localizationService,
        IPathProvider pathProvider
        )
    {
        _preferencesRepository = preferencesRepository;
        _localizationService = localizationService;
        _pathProvider = pathProvider;

        var preferences = preferencesRepository.Load();
        selectedLanguage = NormalizeLanguage(preferences.Language);
        logDirectory = preferences.LogDirectory;

        RefreshTranslations();
    }

    public void SetOnLanguageChanged(Action onLanguageChanged)
    {
        _onLanguageChanged = onLanguageChanged;
    }

    partial void OnLogDirectoryChanged(string? value)
    {
        ValidatePath();
    }

    private void ValidatePath()
    {
        if (string.IsNullOrWhiteSpace(LogDirectory))
        {
            PathError = null;
            return;
        }

        PathError = IsValidPath(LogDirectory)
            ? null
            : _localizationService.TranslateText(LocalizationKey.gui_config_path_invalid);
    }

    private static bool IsValidPath(string path)
    {
        try
        {
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;

            // Doit être un chemin absolu (/home/... sur Linux, C:\... sur Windows)
            if (!Path.IsPathRooted(path))
                return false;

            // GetFullPath lève une exception sur les formats vraiment invalides
            Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void RefreshTranslations()
    {
        TitleText = _localizationService.TranslateText(LocalizationKey.gui_config_title);
        LanguageText = _localizationService.TranslateText(LocalizationKey.gui_config_language);
        LogDirectoryText = _localizationService.TranslateText(LocalizationKey.gui_config_log_directory);
        LogDirectoryWatermark = _localizationService.TranslateText(LocalizationKey.gui_config_log_directory_watermark);
        ResetText = _localizationService.TranslateText(LocalizationKey.gui_config_reset);
        SaveText = _localizationService.TranslateText(LocalizationKey.gui_config_save);
        BrowseText = _localizationService.TranslateText(LocalizationKey.gui_config_browse);
        ValidatePath();
    }

    [RelayCommand]
    private async Task BrowseLogDirectory()
    {
        if (BrowseFolder is null) return;

        var path = await BrowseFolder();
        if (path is not null)
        {
            LogDirectory = path;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        var language = NormalizeLanguage(SelectedLanguage);
        SelectedLanguage = language;

        var preferences = new UserPreferences
        {
            Language = language,
            LogDirectory = string.IsNullOrWhiteSpace(LogDirectory) ? null : LogDirectory
        };
        _preferencesRepository.Save(preferences);

        _localizationService.Culture = language;
        _pathProvider.SetLogDirectoryOverride(preferences.LogDirectory);

        _onLanguageChanged();

        StatusMessage = _localizationService.TranslateText(LocalizationKey.gui_config_saved);
    }

    private bool CanSave() => PathError is null;

    private const string DefaultLanguage = "fr";

    private string NormalizeLanguage(string? language)
    {
        if (language is not null && AvailableLanguages.Any(l => l.Code == language))
            return language;
        return DefaultLanguage;
    }

    partial void OnPathErrorChanged(string? value)
    {
        SaveCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ResetLogDirectory()
    {
        LogDirectory = null;
    }
}

public record LanguageOption(string Code, string Label)
{
    public override string ToString() => Label;
}
