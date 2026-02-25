namespace EasySave.UI.Tests;

public class SettingsFlowServiceAdditionalTests
{
    [Fact]
    public void InitializeCulture_WithValidLanguage_DoesNotPersistAgain()
    {
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "en", LogFormat = LogFormat.Json }
        };
        var localization = new FakeLocalizationService();
        var service = CreateService(repo, localization, out _, out _, out _, out _, out _);

        service.InitializeCulture();

        Assert.Equal("en", localization.Culture);
        Assert.Equal(0, repo.SaveCalls);
    }

    [Fact]
    public void ChangeLocale_WithInvalidLocale_FallsBackToFrench()
    {
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "en", LogFormat = LogFormat.Json }
        };
        var localization = new FakeLocalizationService();
        var service = CreateService(repo, localization, out _, out _, out _, out _, out _);
        var callbackCalled = false;

        service.ChangeLocale("xx", () => callbackCalled = true);

        Assert.True(callbackCalled);
        Assert.Equal("fr", localization.Culture);
        Assert.Equal("fr", repo.Preferences.Language);
    }

    [Fact]
    public void ChangeLocale_WithWhitespace_FallsBackToFrench()
    {
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "en", LogFormat = LogFormat.Json }
        };
        var localization = new FakeLocalizationService();
        var service = CreateService(repo, localization, out _, out _, out _, out _, out _);

        service.ChangeLocale("   ", () => { });

        Assert.Equal("fr", localization.Culture);
        Assert.Equal("fr", repo.Preferences.Language);
    }

    [Fact]
    public void ConfigureParams_LocaleAction_ShowsLocaleMenuAndCanSelectCulture()
    {
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };
        var service = CreateService(repo, new FakeLocalizationService(), out _, out var menuService, out _, out _, out _);
        var callbackCalled = false;

        service.ConfigureParams(() => callbackCalled = true);
        menuService.ShownMenuConfigs[0].Actions[0]();

        var localeMenu = menuService.ShownMenuConfigs[1];
        localeMenu.Actions[0]();

        Assert.True(callbackCalled);
        Assert.Equal("en", repo.Preferences.Language);
    }

    [Fact]
    public void ConfigureParams_LocaleAction_BackReturnsToSettings()
    {
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };
        var service = CreateService(repo, new FakeLocalizationService(), out _, out var menuService, out _, out _, out _);

        service.ConfigureParams(() => { });
        menuService.ShownMenuConfigs[0].Actions[0]();
        var localeMenu = menuService.ShownMenuConfigs[1];
        var backIndex = localeMenu.Actions.Keys.Max();
        localeMenu.Actions[backIndex]();

        Assert.Equal(LocalizationKey.menu_params, menuService.ShownMenuConfigs.Last().Label);
    }

    [Fact]
    public void ShowChangeLogDirectory_WhenInputIsNull_ReturnsToSettingsMenu()
    {
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };
        var service = CreateService(repo, new FakeLocalizationService(), out _, out var menuService, out _, out var inputService, out _);

        inputService.StringAnswers.Enqueue(null);
        service.ConfigureParams(() => { });
        menuService.ShownMenuConfigs[0].Actions[1]();

        Assert.Equal(2, menuService.ShownMenuConfigs.Count);
        Assert.Equal(LocalizationKey.menu_params, menuService.ShownMenuConfigs[1].Label);
    }

    [Fact]
    public void ShowChangeLogDirectory_TrimmedValueIsPersisted()
    {
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };
        var service = CreateService(repo, new FakeLocalizationService(), out var pathProvider, out var menuService, out _, out var inputService, out _);

        inputService.StringAnswers.Enqueue(" logs ");
        service.ConfigureParams(() => { });
        menuService.ShownMenuConfigs[0].Actions[1]();

        Assert.Equal("logs", repo.Preferences.LogDirectory);
        Assert.Equal(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "logs")), pathProvider.LogDirectoryOverride);
    }

    [Fact]
    public void ConfigureParams_LogFormatAction_SelectXml_PersistsPreference()
    {
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };
        var service = CreateService(repo, new FakeLocalizationService(), out _, out var menuService, out _, out _, out _);

        service.ConfigureParams(() => { });
        menuService.ShownMenuConfigs[0].Actions[2]();

        var formatMenu = menuService.ShownMenuConfigs[1];
        formatMenu.Actions[1]();

        Assert.Equal(LogFormat.Xml, repo.Preferences.LogFormat);
    }

    [Fact]
    public void ConfigureParams_LogFormatAction_BackReturnsToSettings()
    {
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };
        var service = CreateService(repo, new FakeLocalizationService(), out _, out var menuService, out _, out _, out _);

        service.ConfigureParams(() => { });
        menuService.ShownMenuConfigs[0].Actions[2]();
        menuService.ShownMenuConfigs[1].Actions[2]();

        Assert.Equal(LocalizationKey.menu_params, menuService.ShownMenuConfigs.Last().Label);
    }

    [Fact]
    public void RenderSettingsHeader_WhenPendingLogFormat_WritesPendingMessage()
    {
        var repo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };
        var service = CreateService(repo, new FakeLocalizationService(), out _, out _, out var messageService, out _, out _);

        service.ChangeLogFormat(LogFormat.Xml, () => { });

        Assert.Contains(
            messageService.WritesWithParams,
            call => call.Key == LocalizationKey.settings_log_format_pending);
    }

    private static SettingsFlowService CreateService(
        FakeUserPreferencesRepository preferencesRepo,
        FakeLocalizationService localization,
        out FakePathProvider pathProvider,
        out FakeMenuService menuService,
        out FakeConsoleMessageService messageService,
        out FakeConsoleInputService inputService,
        out FakeConsoleAdapter consoleAdapter)
    {
        pathProvider = new FakePathProvider();
        menuService = new FakeMenuService();
        messageService = new FakeConsoleMessageService();
        inputService = new FakeConsoleInputService();
        consoleAdapter = new FakeConsoleAdapter();

        return new SettingsFlowService(
            preferencesRepo,
            preferencesRepo.Preferences,
            pathProvider,
            localization,
            menuService,
            new MenuFactory(),
            messageService,
            inputService,
            consoleAdapter);
    }
}
