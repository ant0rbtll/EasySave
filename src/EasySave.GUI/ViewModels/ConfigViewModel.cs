using System.Collections.ObjectModel;
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
    private static readonly char[] ExtensionSeparators = [',', ' '];
    private Action _onLanguageChanged = static() => {};

    /// <summary>
    /// Fonction fournie par la couche UI pour ouvrir le sélecteur de dossier natif.
    /// Retourne le chemin sélectionné ou null si annulé.
    /// </summary>
    public Func<Task<string?>>? BrowseFolder { get; set; }

    [ObservableProperty] private string selectedLanguage;
    [ObservableProperty] private string? logDirectory;
    [ObservableProperty] private string? businessSoftwareProcessName;
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private string? pathError;

    // Propriétés traduites
    [ObservableProperty] private string titleText = "";
    [ObservableProperty] private string languageText = "";
    [ObservableProperty] private string logDirectoryText = "";
    [ObservableProperty] private string logDirectoryWatermark = "";
    [ObservableProperty] private string businessSoftwareText = "";
    [ObservableProperty] private string businessSoftwareWatermark = "";
    [ObservableProperty] private string resetText = "";
    [ObservableProperty] private string saveText = "";
    [ObservableProperty] private string browseText = "";
    [ObservableProperty] private string encryptedExtensionsText = "";
    [ObservableProperty] private string encryptedExtensionsWatermark = "";
    [ObservableProperty] private string encryptedExtensionsAddText = "";
    [ObservableProperty] private string encryptedExtensionsEmptyText = "";

    public ObservableCollection<string> EncryptedExtensions { get; } = [];

    [ObservableProperty] private string? newExtensionInput;

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
        businessSoftwareProcessName = preferences.BusinessSoftwareProcessName;

        foreach (
            var normalized in preferences.EncryptedExtensions
                .Select(NormalizeExtensionForDisplay)
                .Where(n => n is not null)
        )
        {
            EncryptedExtensions.Add(normalized!);
        }

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
        BusinessSoftwareText = _localizationService.TranslateText(LocalizationKey.gui_config_business_software);
        BusinessSoftwareWatermark = _localizationService.TranslateText(LocalizationKey.gui_config_business_software_watermark);
        ResetText = _localizationService.TranslateText(LocalizationKey.gui_config_reset);
        SaveText = _localizationService.TranslateText(LocalizationKey.gui_config_save);
        BrowseText = _localizationService.TranslateText(LocalizationKey.gui_config_browse);
        EncryptedExtensionsText = _localizationService.TranslateText(LocalizationKey.gui_config_encrypted_extensions);
        EncryptedExtensionsWatermark = _localizationService.TranslateText(LocalizationKey.gui_config_encrypted_extensions_watermark);
        EncryptedExtensionsAddText = _localizationService.TranslateText(LocalizationKey.gui_config_encrypted_extensions_add);
        EncryptedExtensionsEmptyText = _localizationService.TranslateText(LocalizationKey.gui_config_encrypted_extensions_empty);
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

        var preferences = _preferencesRepository.Load();
        preferences.Language = language;
        preferences.LogDirectory = string.IsNullOrWhiteSpace(LogDirectory) ? null : LogDirectory;
        preferences.BusinessSoftwareProcessName = string.IsNullOrWhiteSpace(BusinessSoftwareProcessName)
            ? null
            : BusinessSoftwareProcessName.Trim();
        preferences.EncryptedExtensions = [..EncryptedExtensions];
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

    [RelayCommand]
    private void AddExtension()
    {
        if (string.IsNullOrWhiteSpace(NewExtensionInput))
            return;

        var extensions = NewExtensionInput
            .Split(ExtensionSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeExtensionForDisplay)
            .Where(n => n is not null && !EncryptedExtensions.Contains(n));

        foreach (var ext in extensions)
        {
            EncryptedExtensions.Add(ext!);
        }

        NewExtensionInput = null;
    }

    [RelayCommand]
    private void RemoveExtension(string extension)
    {
        EncryptedExtensions.Remove(extension);
    }

    private static string? NormalizeExtensionForDisplay(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        // Keep only alphanumeric chars and the leading dot
        var chars = extension.Trim().ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == '.')
            .ToArray();

        var trimmed = new string(chars).TrimStart('.');
        if (trimmed.Length == 0)
            return null;

        return "." + trimmed;
    }
}

public record LanguageOption(string Code, string Label)
{
    public override string ToString() => Label;
}
