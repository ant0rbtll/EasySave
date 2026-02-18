using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Configuration;
using EasySave.Core;
using EasySave.Localization;
using EasySave.Log;
using EasySave.Persistence;
using static EasySave.GUI.Helpers.PathValidation;

namespace EasySave.GUI.ViewModels;

public partial class ConfigViewModel : ViewModelBase
{
    private const int SavedMessageDurationMs = 3000;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IPathProvider _pathProvider;
    private readonly ILoggerRuntimeReloader _loggerRuntimeReloader;
    private CancellationTokenSource? _statusMessageCts;
    private static readonly char[] ExtensionSeparators = [',', ' '];
    private static readonly char[] BusinessSoftwareSeparators = [',', ';'];
    private Action _onLanguageChanged = static() => {};

    /// <summary>
    /// Fonction fournie par la couche UI pour ouvrir le sélecteur de dossier natif.
    /// Retourne le chemin sélectionné ou null si annulé.
    /// </summary>
    public Func<Task<string?>>? BrowseFolder { get; set; }

    [ObservableProperty] private string selectedLanguage;
    [ObservableProperty] private LogFormat selectedLogFormat;
    [ObservableProperty] private LogMode selectedLogMode;
    [ObservableProperty] private string? logServerUrl;
    [ObservableProperty] private string? logDirectory;
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private string? pathError;

    // Propriétés traduites
    [ObservableProperty] private string titleText = "";
    [ObservableProperty] private string languageText = "";
    [ObservableProperty] private string logFormatText = "";
    [ObservableProperty] private string logFormatJsonText = "";
    [ObservableProperty] private string logFormatXmlText = "";
    [ObservableProperty] private string logDirectoryText = "";
    [ObservableProperty] private string logDirectoryWatermark = "";
    [ObservableProperty] private string businessSoftwareText = "";
    [ObservableProperty] private string businessSoftwareWatermark = "";
    [ObservableProperty] private string businessSoftwareAddText = "";
    [ObservableProperty] private string businessSoftwareEmptyText = "";
    [ObservableProperty] private string resetText = "";
    [ObservableProperty] private string saveText = "";
    [ObservableProperty] private string browseText = "";
    [ObservableProperty] private string encryptedExtensionsText = "";
    [ObservableProperty] private string encryptedExtensionsWatermark = "";
    [ObservableProperty] private string encryptedExtensionsAddText = "";
    [ObservableProperty] private string encryptedExtensionsEmptyText = "";
    [ObservableProperty] private string logModeText = "";
    [ObservableProperty] private string logModeLocalText = "";
    [ObservableProperty] private string logModeCentralizedText = "";
    [ObservableProperty] private string logModeLocalAndCentralizedText = "";
    [ObservableProperty] private string logServerUrlText = "";
    [ObservableProperty] private string logServerUrlWatermark = "";

    public ObservableCollection<string> EncryptedExtensions { get; } = [];
    public ObservableCollection<string> BusinessSoftwareProcesses { get; } = [];

    public bool IsJsonLogFormatSelected => SelectedLogFormat == LogFormat.Json;
    public bool IsXmlLogFormatSelected => SelectedLogFormat == LogFormat.Xml;

    public bool IsLocalModeSelected => SelectedLogMode == LogMode.Local;
    public bool IsCentralizedModeSelected => SelectedLogMode == LogMode.Centralized;
    public bool IsLocalAndCentralizedModeSelected => SelectedLogMode == LogMode.LocalAndCentralized;
    public bool IsLogServerUrlVisible => SelectedLogMode != LogMode.Local;

    [ObservableProperty] private string? newExtensionInput;
    [ObservableProperty] private string? newBusinessSoftwareInput;

    public ObservableCollection<LanguageOption> AvailableLanguages { get; } =
    [
        new LanguageOption("fr", "Français"),
        new LanguageOption("en", "English"),
    ];

    public ConfigViewModel(
        IUserPreferencesRepository preferencesRepository,
        ILocalizationService localizationService,
        IPathProvider pathProvider,
        ILoggerRuntimeReloader loggerRuntimeReloader
        )
    {
        _preferencesRepository = preferencesRepository;
        _localizationService = localizationService;
        _pathProvider = pathProvider;
        _loggerRuntimeReloader = loggerRuntimeReloader;

        var preferences = preferencesRepository.Load();
        selectedLanguage = NormalizeLanguage(preferences.Language);
        selectedLogFormat = preferences.LogFormat;
        selectedLogMode = preferences.LogMode;
        logServerUrl = preferences.LogServerUrl;
        logDirectory = preferences.LogDirectory;

        foreach (var processName in preferences.BusinessSoftwareProcessNames)
        {
            BusinessSoftwareProcesses.Add(processName);
        }

        foreach (
            var normalized in preferences.EncryptedExtensions
                .Select(NormalizeExtensionForDisplay)
                .Where(n => n is not null)
        )
        {
            EncryptedExtensions.Add(normalized!);
        }

        EncryptedExtensions.CollectionChanged += OnConfigCollectionChanged;
        BusinessSoftwareProcesses.CollectionChanged += OnConfigCollectionChanged;

        RefreshTranslations();
    }

    public void SetOnLanguageChanged(Action onLanguageChanged)
    {
        _onLanguageChanged = onLanguageChanged;
    }

    partial void OnLogDirectoryChanged(string? value)
    {
        ClearStatusMessage();
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


    public void RefreshTranslations()
    {
        TitleText = _localizationService.TranslateText(LocalizationKey.gui_config_title);
        LanguageText = _localizationService.TranslateText(LocalizationKey.gui_config_language);
        LogFormatText = _localizationService.TranslateText(LocalizationKey.gui_config_log_format);
        LogFormatJsonText = _localizationService.TranslateText(LocalizationKey.log_format_json);
        LogFormatXmlText = _localizationService.TranslateText(LocalizationKey.log_format_xml);
        LogDirectoryText = _localizationService.TranslateText(LocalizationKey.gui_config_log_directory);
        LogDirectoryWatermark = _localizationService.TranslateText(LocalizationKey.gui_config_log_directory_watermark);
        BusinessSoftwareText = _localizationService.TranslateText(LocalizationKey.gui_config_business_software);
        BusinessSoftwareWatermark = _localizationService.TranslateText(LocalizationKey.gui_config_business_software_watermark);
        BusinessSoftwareAddText = _localizationService.TranslateText(LocalizationKey.gui_config_business_software_add);
        BusinessSoftwareEmptyText = _localizationService.TranslateText(LocalizationKey.gui_config_business_software_empty);
        ResetText = _localizationService.TranslateText(LocalizationKey.gui_config_reset);
        SaveText = _localizationService.TranslateText(LocalizationKey.gui_config_save);
        BrowseText = _localizationService.TranslateText(LocalizationKey.gui_config_browse);
        EncryptedExtensionsText = _localizationService.TranslateText(LocalizationKey.gui_config_encrypted_extensions);
        EncryptedExtensionsWatermark = _localizationService.TranslateText(LocalizationKey.gui_config_encrypted_extensions_watermark);
        EncryptedExtensionsAddText = _localizationService.TranslateText(LocalizationKey.gui_config_encrypted_extensions_add);
        EncryptedExtensionsEmptyText = _localizationService.TranslateText(LocalizationKey.gui_config_encrypted_extensions_empty);
        LogModeText = _localizationService.TranslateText(LocalizationKey.gui_config_log_mode);
        LogModeLocalText = _localizationService.TranslateText(LocalizationKey.log_mode_local);
        LogModeCentralizedText = _localizationService.TranslateText(LocalizationKey.log_mode_centralized);
        LogModeLocalAndCentralizedText = _localizationService.TranslateText(LocalizationKey.log_mode_local_and_centralized);
        LogServerUrlText = _localizationService.TranslateText(LocalizationKey.gui_config_log_server_url);
        LogServerUrlWatermark = _localizationService.TranslateText(LocalizationKey.gui_config_log_server_url_watermark);
        ValidatePath();
    }

    partial void OnSelectedLogFormatChanged(LogFormat value)
    {
        ClearStatusMessage();
        OnPropertyChanged(nameof(IsJsonLogFormatSelected));
        OnPropertyChanged(nameof(IsXmlLogFormatSelected));
    }

    partial void OnSelectedLogModeChanged(LogMode value)
    {
        ClearStatusMessage();
        OnPropertyChanged(nameof(IsLocalModeSelected));
        OnPropertyChanged(nameof(IsCentralizedModeSelected));
        OnPropertyChanged(nameof(IsLocalAndCentralizedModeSelected));
        OnPropertyChanged(nameof(IsLogServerUrlVisible));
    }

    partial void OnLogServerUrlChanged(string? value)
    {
        ClearStatusMessage();
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        ClearStatusMessage();
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
        var logFormatChanged = preferences.LogFormat != SelectedLogFormat;
        var logModeChanged = preferences.LogMode != SelectedLogMode;
        var logServerUrlChanged = preferences.LogServerUrl != LogServerUrl;
        preferences.Language = language;
        preferences.LogFormat = SelectedLogFormat;
        preferences.LogMode = SelectedLogMode;
        preferences.LogServerUrl = string.IsNullOrWhiteSpace(LogServerUrl) ? null : LogServerUrl;
        preferences.LogDirectory = string.IsNullOrWhiteSpace(LogDirectory) ? null : LogDirectory;
        preferences.BusinessSoftwareProcessNames = [..BusinessSoftwareProcesses];
        preferences.EncryptedExtensions = [..EncryptedExtensions];
        _preferencesRepository.Save(preferences);

        _localizationService.Culture = language;
        _pathProvider.SetLogDirectoryOverride(preferences.LogDirectory);
        if (logFormatChanged || logModeChanged || logServerUrlChanged)
        {
            _loggerRuntimeReloader.Reload();
        }

        _onLanguageChanged();

        StatusMessage = _localizationService.TranslateText(LocalizationKey.gui_config_saved);
        RestartStatusMessageAutoClear();
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
    private void SelectJsonLogFormat()
    {
        SelectedLogFormat = LogFormat.Json;
    }

    [RelayCommand]
    private void SelectXmlLogFormat()
    {
        SelectedLogFormat = LogFormat.Xml;
    }

    [RelayCommand]
    private void SelectLocalMode()
    {
        SelectedLogMode = LogMode.Local;
    }

    [RelayCommand]
    private void SelectCentralizedMode()
    {
        SelectedLogMode = LogMode.Centralized;
    }

    [RelayCommand]
    private void SelectLocalAndCentralizedMode()
    {
        SelectedLogMode = LogMode.LocalAndCentralized;
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
        ClearStatusMessage();
    }

    [RelayCommand]
    private void RemoveExtension(string extension)
    {
        EncryptedExtensions.Remove(extension);
        ClearStatusMessage();
    }

    [RelayCommand]
    private void AddBusinessSoftware()
    {
        if (string.IsNullOrWhiteSpace(NewBusinessSoftwareInput))
            return;

        var existingNormalized = new HashSet<string>(
            BusinessSoftwareProcesses.Select(NormalizeBusinessSoftwareForComparison),
            StringComparer.OrdinalIgnoreCase
        );

        var toAdd = NewBusinessSoftwareInput
            .Split(BusinessSoftwareSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeBusinessSoftwareForDisplay)
            .Where(name => name is not null)
            .Cast<string>()
            .Where(name => existingNormalized.Add(NormalizeBusinessSoftwareForComparison(name)));

        foreach (var processName in toAdd)
        {
            BusinessSoftwareProcesses.Add(processName);
        }

        NewBusinessSoftwareInput = null;
        ClearStatusMessage();
    }

    [RelayCommand]
    private void RemoveBusinessSoftware(string processName)
    {
        BusinessSoftwareProcesses.Remove(processName);
        ClearStatusMessage();
    }

    private void OnConfigCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ClearStatusMessage();
    }

    private void RestartStatusMessageAutoClear()
    {
        _statusMessageCts?.Cancel();
        _statusMessageCts?.Dispose();
        _statusMessageCts = new CancellationTokenSource();

        _ = ClearStatusMessageAfterDelayAsync(_statusMessageCts.Token);
    }

    private async Task ClearStatusMessageAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(SavedMessageDurationMs, cancellationToken);
            StatusMessage = null;
        }
        catch (OperationCanceledException)
        {
            // The delay is cancelled when a new save or edit occurs.
        }
    }

    private void ClearStatusMessage()
    {
        if (StatusMessage is null)
            return;

        _statusMessageCts?.Cancel();
        _statusMessageCts?.Dispose();
        _statusMessageCts = null;
        StatusMessage = null;
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

    private static string? NormalizeBusinessSoftwareForDisplay(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return null;

        var trimmed = processName.Trim();
        var fileName = Path.GetFileName(trimmed);
        var withoutExtension = Path.GetFileNameWithoutExtension(fileName);
        return string.IsNullOrWhiteSpace(withoutExtension) ? trimmed : withoutExtension;
    }

    private static string NormalizeBusinessSoftwareForComparison(string processName)
    {
        var normalized = NormalizeBusinessSoftwareForDisplay(processName);
        return normalized ?? string.Empty;
    }

}

public record LanguageOption(string Code, string Label)
{
    public override string ToString() => Label;
}
