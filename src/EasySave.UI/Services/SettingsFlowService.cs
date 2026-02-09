using EasySave.Configuration;
using EasySave.Core;
using EasySave.Localization;
using EasySave.Persistence;
using EasySave.UI.Menu;

namespace EasySave.UI.Services;

/// <summary>
/// Handles the settings navigation and persistence flow.
/// </summary>
internal class SettingsFlowService(
    IUserPreferencesRepository preferencesRepository,
    UserPreferences userPreferences,
    IPathProvider pathProvider,
    ILocalizationService localizationService,
    IMenuService menuService,
    IMenuFactory menuFactory,
    IConsoleMessageService messageService,
    IConsoleInputService inputService,
    IConsoleAdapter consoleAdapter)
{
    private readonly IUserPreferencesRepository _preferencesRepository = preferencesRepository;
    private readonly UserPreferences _userPreferences = userPreferences;
    private readonly IPathProvider _pathProvider = pathProvider;
    private readonly ILocalizationService _localizationService = localizationService;
    private readonly IMenuService _menuService = menuService;
    private readonly IMenuFactory _menuFactory = menuFactory;
    private readonly IConsoleMessageService _messageService = messageService;
    private readonly IConsoleInputService _inputService = inputService;
    private readonly IConsoleAdapter _consoleAdapter = consoleAdapter;
    private readonly LogFormat _activeLogFormat = userPreferences.LogFormat;

    private string _activeLogDirectory = string.Empty;
    private bool _isUsingDefaultLogDirectory;

    /// <summary>
    /// Initializes culture and log directory settings from persisted preferences.
    /// </summary>
    public void InitializeCulture()
    {
        ApplyLogDirectoryPreference(_userPreferences.LogDirectory);

        var language = _userPreferences.Language;
        if (string.IsNullOrWhiteSpace(language) || !_localizationService.AllCultures.ContainsKey(language))
        {
            language = "fr";
            _userPreferences.Language = language;
            _preferencesRepository.Save(_userPreferences);
        }

        _localizationService.Culture = language;
    }

    /// <summary>
    /// Displays the settings root menu.
    /// </summary>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    public void ConfigureParams(Action onBackToMainMenu)
    {
        var menuConfig = _menuFactory.CreateParamsMenu(
            () => ShowChangeLocale(onBackToMainMenu),
            () => ShowChangeLogDirectory(onBackToMainMenu),
            () => ShowChangeLogFormat(onBackToMainMenu),
            onBackToMainMenu,
            RenderSettingsHeader);

        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Displays the locale change menu.
    /// </summary>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    private void ShowChangeLocale(Action onBackToMainMenu)
    {
        var menuConfig = _menuFactory.CreateLocaleMenu(
            _localizationService.AllCultures,
            locale => ChangeLocale(locale, onBackToMainMenu),
            () => ConfigureParams(onBackToMainMenu),
            RenderLocaleHeader);

        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Displays and processes the log directory prompt.
    /// </summary>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    private void ShowChangeLogDirectory(Action onBackToMainMenu)
    {
        _consoleAdapter.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_params_log_path);
        DisplayActiveLogDirectoryStatus();
        _consoleAdapter.WriteLine();

        var input = _inputService.AskString(LocalizationKey.ask_log_path);
        if (input == null)
        {
            ConfigureParams(onBackToMainMenu);
            return;
        }

        if (input.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            ChangeLogDirectory(null, onBackToMainMenu);
            return;
        }

        if (!IsValidPath(input))
        {
            _messageService.Write(LocalizationKey.log_path_invalid);
            _menuService.WaitForUser();
            ConfigureParams(onBackToMainMenu);
            return;
        }

        ChangeLogDirectory(input, onBackToMainMenu);
    }

    /// <summary>
    /// Changes the active locale and persists the preference.
    /// </summary>
    /// <param name="locale">Requested locale code.</param>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    public void ChangeLocale(string locale, Action onBackToMainMenu)
    {
        if (string.IsNullOrWhiteSpace(locale) || !_localizationService.AllCultures.ContainsKey(locale))
        {
            locale = "fr";
        }

        _localizationService.Culture = locale;
        _userPreferences.Language = locale;
        _preferencesRepository.Save(_userPreferences);

        onBackToMainMenu();
    }

    /// <summary>
    /// Persists a new log directory and returns to settings.
    /// </summary>
    /// <param name="directory">Requested directory, or <see langword="null"/> for default.</param>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    private void ChangeLogDirectory(string? directory, Action onBackToMainMenu)
    {
        string? normalizedDirectory = string.IsNullOrWhiteSpace(directory) ? null : directory.Trim();

        ApplyLogDirectoryPreference(normalizedDirectory);
        _userPreferences.LogDirectory = normalizedDirectory;
        _preferencesRepository.Save(_userPreferences);

        if (normalizedDirectory == null)
        {
            _messageService.Write(LocalizationKey.log_path_reset);
        }
        else
        {
            _messageService.Write(LocalizationKey.log_path_updated);
        }

        _menuService.WaitForUser();
        ConfigureParams(onBackToMainMenu);
    }

    /// <summary>
    /// Displays the log format change menu.
    /// </summary>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    private void ShowChangeLogFormat(Action onBackToMainMenu)
    {
        var menuConfig = _menuFactory.CreateLogFormatMenu(
            GetLogFormatLabel(LogFormat.Json),
            GetLogFormatLabel(LogFormat.Xml),
            _messageService.Translate(LocalizationKey.back),
            () => ChangeLogFormat(LogFormat.Json, onBackToMainMenu),
            () => ChangeLogFormat(LogFormat.Xml, onBackToMainMenu),
            () => ConfigureParams(onBackToMainMenu),
            RenderLogFormatHeader);

        _menuService.ShowMenuWithActions(menuConfig);
    }

    /// <summary>
    /// Changes the persisted log format preference.
    /// </summary>
    /// <param name="format">New log format.</param>
    /// <param name="onBackToMainMenu">Callback to return to the main menu.</param>
    public void ChangeLogFormat(LogFormat format, Action onBackToMainMenu)
    {
        _userPreferences.LogFormat = format;
        _preferencesRepository.Save(_userPreferences);

        _messageService.Write(LocalizationKey.log_format_updated);
        _messageService.Write(LocalizationKey.log_format_restart_required);
        _menuService.WaitForUser();
        ConfigureParams(onBackToMainMenu);
    }

    /// <summary>
    /// Renders settings context above the settings menu.
    /// </summary>
    private void RenderSettingsHeader()
    {
        _messageService.WriteWithParams(LocalizationKey.settings_current_language, [GetCurrentLanguageLabel()]);
        _messageService.WriteWithParams(LocalizationKey.settings_log_format_active, [GetLogFormatLabel(_activeLogFormat)]);

        if (_userPreferences.LogFormat != _activeLogFormat)
        {
            _messageService.WriteWithParams(LocalizationKey.settings_log_format_pending, [GetLogFormatLabel(_userPreferences.LogFormat)]);
        }

        DisplayActiveLogDirectoryStatus();
        _consoleAdapter.WriteLine();
    }

    /// <summary>
    /// Renders locale context above the locale menu.
    /// </summary>
    private void RenderLocaleHeader()
    {
        _messageService.WriteWithParams(LocalizationKey.settings_current_language, [GetCurrentLanguageLabel()]);
        _consoleAdapter.WriteLine();
    }

    /// <summary>
    /// Renders log format context above the log format menu.
    /// </summary>
    private void RenderLogFormatHeader()
    {
        _messageService.WriteWithParams(LocalizationKey.settings_log_format_active, [GetLogFormatLabel(_activeLogFormat)]);

        if (_userPreferences.LogFormat != _activeLogFormat)
        {
            _messageService.WriteWithParams(LocalizationKey.settings_log_format_pending, [GetLogFormatLabel(_userPreferences.LogFormat)]);
        }

        _consoleAdapter.WriteLine();
    }

    /// <summary>
    /// Applies a log directory preference to runtime state.
    /// </summary>
    /// <param name="directory">Directory preference, or <see langword="null"/> for default.</param>
    private void ApplyLogDirectoryPreference(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            _pathProvider.SetLogDirectoryOverride(null);
            SetDefaultLogDirectoryAsActive();
            return;
        }

        if (!IsValidPath(directory))
        {
            _pathProvider.SetLogDirectoryOverride(null);
            SetDefaultLogDirectoryAsActive();
            return;
        }

        _activeLogDirectory = ResolveLogDirectoryCandidate(directory);
        _pathProvider.SetLogDirectoryOverride(_activeLogDirectory);
        _isUsingDefaultLogDirectory = false;
    }

    /// <summary>
    /// Validates whether a directory path can be used for logs.
    /// </summary>
    /// <param name="path">Path to validate.</param>
    /// <returns><see langword="true"/> when the path is usable; otherwise <see langword="false"/>.</returns>
    private static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return false;
        }

        try
        {
            string candidate = ResolveLogDirectoryCandidate(path);
            Directory.CreateDirectory(candidate);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves relative log paths against the application base directory.
    /// </summary>
    /// <param name="directory">Raw directory value.</param>
    /// <returns>Absolute candidate directory path.</returns>
    private static string ResolveLogDirectoryCandidate(string directory)
    {
        var trimmed = directory.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            return trimmed;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, trimmed));
    }

    /// <summary>
    /// Marks the default log directory as active.
    /// </summary>
    private void SetDefaultLogDirectoryAsActive()
    {
        _activeLogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        _isUsingDefaultLogDirectory = true;
    }

    /// <summary>
    /// Displays the active log directory status message.
    /// </summary>
    private void DisplayActiveLogDirectoryStatus()
    {
        if (_isUsingDefaultLogDirectory)
        {
            _messageService.WriteWithParams(LocalizationKey.settings_log_directory_active_default, [_activeLogDirectory]);
            return;
        }

        _messageService.WriteWithParams(LocalizationKey.settings_log_directory_active_custom, [_activeLogDirectory]);
    }

    /// <summary>
    /// Returns the localized label for the current language.
    /// </summary>
    /// <returns>Localized language label or raw culture code.</returns>
    private string GetCurrentLanguageLabel()
    {
        if (_localizationService.AllCultures.TryGetValue(_localizationService.Culture, out var cultureKey))
        {
            return _localizationService.TranslateText(cultureKey);
        }

        return _localizationService.Culture;
    }

    /// <summary>
    /// Returns the localized label for a log format.
    /// </summary>
    /// <param name="format">Format to label.</param>
    /// <returns>Localized format label.</returns>
    private string GetLogFormatLabel(LogFormat format)
    {
        return format switch
        {
            LogFormat.Xml => _localizationService.TranslateText(LocalizationKey.log_format_xml),
            _ => _localizationService.TranslateText(LocalizationKey.log_format_json)
        };
    }
}
