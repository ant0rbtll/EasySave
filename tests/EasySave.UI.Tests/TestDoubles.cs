namespace EasySave.UI.Tests;

internal sealed class FakeLocalizationService : ILocalizationService
{
    public string Culture { get; set; } = "fr";

    public Dictionary<string, LocalizationKey> AllCultures { get; } = new()
    {
        { "fr", LocalizationKey.config_locale_fr },
        { "en", LocalizationKey.config_locale_en }
    };

    public Dictionary<LocalizationKey, string> KeyTranslations { get; } = new();

    public string TranslateText(string key)
    {
        return key;
    }

    public string TranslateTextWithParams(LocalizationKey key, string[] parameters)
    {
        return $"{key}:{string.Join(",", parameters)}";
    }

    public string TranslateText(LocalizationKey key)
    {
        if (KeyTranslations.TryGetValue(key, out var value))
        {
            return value;
        }

        return key.ToString();
    }
}

internal sealed class FakeConsoleAdapter : IConsoleAdapter
{
    private readonly Queue<ConsoleKeyInfo> _keys = new();

    public List<string> Events { get; } = new();

    public void EnqueueKey(ConsoleKey key, char keyChar = '\0')
    {
        _keys.Enqueue(new ConsoleKeyInfo(keyChar, key, false, false, false));
    }

    public void Clear() => Events.Add("CLEAR");

    public void Write(string value) => Events.Add($"W:{value}");

    public void WriteLine() => Events.Add("WL:");

    public void WriteLine(string value) => Events.Add($"WL:{value}");

    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        if (_keys.Count == 0)
        {
            throw new InvalidOperationException("No queued key available.");
        }

        return _keys.Dequeue();
    }

    public void SetForegroundColor(ConsoleColor color) => Events.Add($"FG:{color}");

    public void ResetColor() => Events.Add("RESET");

    public void WriteErrorLine(string value) => Events.Add($"ERR:{value}");
}

internal sealed class FakeMenuService : IMenuService
{
    public List<MenuConfig> ShownMenuConfigs { get; } = new();
    public List<LocalizationKey> DisplayedLabels { get; } = new();
    public int WaitCalls { get; private set; }
    public int ShowMenuResult { get; set; } = 2;

    public int ShowMenu(LocalizationKey[] menuItems, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null)
    {
        renderHeader?.Invoke();
        return ShowMenuResult;
    }

    public int ShowMenu(string[] menuItems, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null)
    {
        renderHeader?.Invoke();
        return ShowMenuResult;
    }

    public void ShowMenuWithActions(LocalizationKey[] menuItems, Dictionary<int, Action> menuActions, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null)
    {
        ShowMenuWithActions(new MenuConfig(menuItems, menuActions, menuLabel, renderHeader));
    }

    public void ShowMenuWithActions(string[] menuItems, Dictionary<int, Action> menuActions, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null)
    {
        ShowMenuWithActions(new MenuConfig(menuItems, menuActions, menuLabel, renderHeader));
    }

    public void ShowMenuWithActions(MenuConfig menuConfig)
    {
        ShownMenuConfigs.Add(menuConfig);
        menuConfig.RenderHeader?.Invoke();
    }

    public void DisplayLabel(LocalizationKey key)
    {
        DisplayedLabels.Add(key);
    }

    public void WaitForUser(LocalizationKey messageKey = LocalizationKey.waiting_user)
    {
        WaitCalls++;
    }
}

internal sealed class FakePathProvider : IPathProvider
{
    public string? LogDirectoryOverride { get; private set; }

    public string GetDailyLogPath(DateTime date, LogFormat format = LogFormat.Json) => "log";

    public string GetStatePath() => "state";

    public string GetJobsConfigPath() => "jobs";

    public string GetUserPreferencesPath() => "prefs";

    public void SetLogDirectoryOverride(string? directory)
    {
        LogDirectoryOverride = directory;
    }

    public string ResolveLogsDirectory() => "logs";
}

internal sealed class FakeUserPreferencesRepository : IUserPreferencesRepository
{
    public UserPreferences Preferences { get; set; } = new();
    public int SaveCalls { get; private set; }

    public UserPreferences Load() => Preferences;

    public void Save(UserPreferences preferences)
    {
        SaveCalls++;
        Preferences = preferences;
    }
}

internal sealed class FakeConsoleMessageService : IConsoleMessageService
{
    public List<(LocalizationKey Key, bool WriteLine)> Writes { get; } = new();
    public List<(LocalizationKey Key, string[] Parameters, bool WriteLine)> WritesWithParams { get; } = new();
    public List<Exception> Errors { get; } = new();

    public Dictionary<LocalizationKey, string> Translations { get; } = new();

    public void Write(LocalizationKey key, bool writeLine = true)
    {
        Writes.Add((key, writeLine));
    }

    public void WriteWithParams(LocalizationKey key, string[] parameters, bool writeLine = true)
    {
        WritesWithParams.Add((key, parameters, writeLine));
    }

    public string Translate(LocalizationKey key)
    {
        if (Translations.TryGetValue(key, out var value))
        {
            return value;
        }

        return key.ToString();
    }

    public void ShowError(Exception exception)
    {
        Errors.Add(exception);
    }
}

internal sealed class FakeConsoleInputService : IConsoleInputService
{
    public Queue<string?> StringAnswers { get; } = new();
    public Queue<int?> IntAnswers { get; } = new();
    public Queue<string?> StringWithCurrentAnswers { get; } = new();
    public Queue<int?> IntWithCurrentAnswers { get; } = new();
    public Queue<BackupType?> BackupTypeAnswers { get; } = new();
    public Queue<BackupType?> BackupTypeWithCurrentAnswers { get; } = new();

    public string? AskString(LocalizationKey key) => StringAnswers.Dequeue();

    public int? AskInt(LocalizationKey key) => IntAnswers.Dequeue();

    public string? AskStringWithCurrentValue(LocalizationKey key, string currentValue) => StringWithCurrentAnswers.Dequeue();

    public int? AskIntWithCurrentValue(LocalizationKey key, int currentValue) => IntWithCurrentAnswers.Dequeue();

    public BackupType? AskBackupType(LocalizationKey key) => BackupTypeAnswers.Dequeue();

    public BackupType? AskBackupTypeWithCurrentValue(LocalizationKey key, BackupType currentType) => BackupTypeWithCurrentAnswers.Dequeue();
}

internal sealed class FakeBackupEngine : IBackupEngine
{
    public List<BackupJob> ExecutedJobs { get; } = new();

    public void Execute(BackupJob job)
    {
        ExecutedJobs.Add(job);
    }
}
