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
    IConsoleInputService inputService)
{
    private readonly IUserPreferencesRepository _preferencesRepository = preferencesRepository;
    private readonly UserPreferences _userPreferences = userPreferences;
    private readonly IPathProvider _pathProvider = pathProvider;
    private readonly ILocalizationService _localizationService = localizationService;
    private readonly IMenuService _menuService = menuService;
    private readonly IMenuFactory _menuFactory = menuFactory;
    private readonly IConsoleMessageService _messageService = messageService;
    private readonly IConsoleInputService _inputService = inputService;
    private readonly LogFormat _activeLogFormat = userPreferences.LogFormat;

    private string _activeLogDirectory = string.Empty;
    private bool _isUsingDefaultLogDirectory;

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

    private void ShowChangeLocale(Action onBackToMainMenu)
    {
        var menuConfig = _menuFactory.CreateLocaleMenu(
            _localizationService.AllCultures,
            locale => ChangeLocale(locale, onBackToMainMenu),
            () => ConfigureParams(onBackToMainMenu),
            RenderLocaleHeader);

        _menuService.ShowMenuWithActions(menuConfig);
    }

    private void ShowChangeLogDirectory(Action onBackToMainMenu)
    {
        Console.Clear();
        _menuService.DisplayLabel(LocalizationKey.menu_params_log_path);
        DisplayActiveLogDirectoryStatus();
        Console.WriteLine();

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

    private void ChangeLogDirectory(string? directory, Action onBackToMainMenu)
    {
        ApplyLogDirectoryPreference(directory);
        _userPreferences.LogDirectory = string.IsNullOrWhiteSpace(directory) ? null : directory;
        _preferencesRepository.Save(_userPreferences);

        if (string.IsNullOrWhiteSpace(directory))
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

    public void ChangeLogFormat(LogFormat format, Action onBackToMainMenu)
    {
        _userPreferences.LogFormat = format;
        _preferencesRepository.Save(_userPreferences);

        _messageService.Write(LocalizationKey.log_format_updated);
        _messageService.Write(LocalizationKey.log_format_restart_required);
        _menuService.WaitForUser();
        ConfigureParams(onBackToMainMenu);
    }

    private void RenderSettingsHeader()
    {
        _messageService.WriteWithParams(LocalizationKey.settings_current_language, [GetCurrentLanguageLabel()]);
        _messageService.WriteWithParams(LocalizationKey.settings_log_format_active, [GetLogFormatLabel(_activeLogFormat)]);

        if (_userPreferences.LogFormat != _activeLogFormat)
        {
            _messageService.WriteWithParams(LocalizationKey.settings_log_format_pending, [GetLogFormatLabel(_userPreferences.LogFormat)]);
        }

        DisplayActiveLogDirectoryStatus();
        Console.WriteLine();
    }

    private void RenderLocaleHeader()
    {
        _messageService.WriteWithParams(LocalizationKey.settings_current_language, [GetCurrentLanguageLabel()]);
        Console.WriteLine();
    }

    private void RenderLogFormatHeader()
    {
        _messageService.WriteWithParams(LocalizationKey.settings_log_format_active, [GetLogFormatLabel(_activeLogFormat)]);

        if (_userPreferences.LogFormat != _activeLogFormat)
        {
            _messageService.WriteWithParams(LocalizationKey.settings_log_format_pending, [GetLogFormatLabel(_userPreferences.LogFormat)]);
        }

        Console.WriteLine();
    }

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

        _pathProvider.SetLogDirectoryOverride(directory);
        _activeLogDirectory = ResolveLogDirectoryCandidate(directory);
        _isUsingDefaultLogDirectory = false;
    }

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

    private static string ResolveLogDirectoryCandidate(string directory)
    {
        var trimmed = directory.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            return trimmed;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, trimmed));
    }

    private void SetDefaultLogDirectoryAsActive()
    {
        _activeLogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        _isUsingDefaultLogDirectory = true;
    }

    private void DisplayActiveLogDirectoryStatus()
    {
        if (_isUsingDefaultLogDirectory)
        {
            _messageService.WriteWithParams(LocalizationKey.settings_log_directory_active_default, [_activeLogDirectory]);
            return;
        }

        _messageService.WriteWithParams(LocalizationKey.settings_log_directory_active_custom, [_activeLogDirectory]);
    }

    private string GetCurrentLanguageLabel()
    {
        if (_localizationService.AllCultures.TryGetValue(_localizationService.Culture, out var cultureKey))
        {
            return _localizationService.TranslateText(cultureKey);
        }

        return _localizationService.Culture;
    }

    private string GetLogFormatLabel(LogFormat format)
    {
        return format switch
        {
            LogFormat.Xml => _localizationService.TranslateText(LocalizationKey.log_format_xml),
            _ => _localizationService.TranslateText(LocalizationKey.log_format_json)
        };
    }
}
