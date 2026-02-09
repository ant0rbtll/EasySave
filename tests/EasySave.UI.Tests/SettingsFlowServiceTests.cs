namespace EasySave.UI.Tests;

public class SettingsFlowServiceTests
{
    [Fact]
    public void InitializeCulture_FallsBackToFrenchWhenLanguageIsInvalid()
    {
        var preferencesRepo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "zz", LogFormat = LogFormat.Json }
        };

        var localization = new FakeLocalizationService();
        var service = CreateService(preferencesRepo, localization, out _, out _, out _, out _, out _);

        service.InitializeCulture();

        Assert.Equal("fr", localization.Culture);
        Assert.Equal("fr", preferencesRepo.Preferences.Language);
        Assert.Equal(1, preferencesRepo.SaveCalls);
    }

    [Fact]
    public void ChangeLocale_UpdatesPreferenceAndInvokesCallback()
    {
        var preferencesRepo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };

        var localization = new FakeLocalizationService();
        var service = CreateService(preferencesRepo, localization, out _, out _, out _, out _, out _);

        var callbackCalled = false;
        service.ChangeLocale("en", () => callbackCalled = true);

        Assert.True(callbackCalled);
        Assert.Equal("en", localization.Culture);
        Assert.Equal("en", preferencesRepo.Preferences.Language);
        Assert.Equal(1, preferencesRepo.SaveCalls);
    }

    [Fact]
    public void ConfigureParams_BuildsSettingsMenuWithBackAction()
    {
        var preferencesRepo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };

        var service = CreateService(preferencesRepo, new FakeLocalizationService(), out _, out var menuService, out _, out _, out _);

        var backCalled = false;
        service.ConfigureParams(() => backCalled = true);

        var menuConfig = Assert.Single(menuService.ShownMenuConfigs);
        Assert.Equal(LocalizationKey.menu_params, menuConfig.Label);

        menuConfig.Actions[3]();

        Assert.True(backCalled);
    }

    [Fact]
    public void ChangeLogFormat_PersistsAndReturnsToSettingsMenu()
    {
        var preferencesRepo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };

        var service = CreateService(preferencesRepo, new FakeLocalizationService(), out _, out var menuService, out var messageService, out _, out _);

        service.ChangeLogFormat(LogFormat.Xml, () => { });

        Assert.Equal(LogFormat.Xml, preferencesRepo.Preferences.LogFormat);
        Assert.Equal(1, preferencesRepo.SaveCalls);
        Assert.Equal(1, menuService.WaitCalls);
        Assert.Contains(messageService.Writes, call => call.Key == LocalizationKey.log_format_updated);
        Assert.Contains(menuService.ShownMenuConfigs, config => config.Label == LocalizationKey.menu_params);
    }

    [Fact]
    public void ShowChangeLogDirectory_WithInvalidPath_ShowsErrorAndReturnsToSettings()
    {
        var preferencesRepo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };

        var service = CreateService(
            preferencesRepo,
            new FakeLocalizationService(),
            out var pathProvider,
            out var menuService,
            out var messageService,
            out var inputService,
            out _);

        service.InitializeCulture();
        inputService.StringAnswers.Enqueue("bad\0path");

        service.ConfigureParams(() => { });
        var settingsMenu = menuService.ShownMenuConfigs[0];
        settingsMenu.Actions[1]();

        Assert.Equal(1, menuService.WaitCalls);
        Assert.Contains(messageService.Writes, call => call.Key == LocalizationKey.log_path_invalid);
        Assert.Null(pathProvider.LogDirectoryOverride);
        Assert.Equal(2, menuService.ShownMenuConfigs.Count);
        Assert.Equal(LocalizationKey.menu_params, menuService.ShownMenuConfigs[1].Label);
    }

    [Fact]
    public void ShowChangeLogDirectory_WithDefault_ResetsOverrideAndPersists()
    {
        var preferencesRepo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json, LogDirectory = "custom" }
        };

        var service = CreateService(
            preferencesRepo,
            new FakeLocalizationService(),
            out var pathProvider,
            out var menuService,
            out var messageService,
            out var inputService,
            out _);

        service.InitializeCulture();
        inputService.StringAnswers.Enqueue("default");

        service.ConfigureParams(() => { });
        var settingsMenu = menuService.ShownMenuConfigs[0];
        settingsMenu.Actions[1]();

        Assert.Null(pathProvider.LogDirectoryOverride);
        Assert.Null(preferencesRepo.Preferences.LogDirectory);
        Assert.Contains(messageService.Writes, call => call.Key == LocalizationKey.log_path_reset);
    }

    [Fact]
    public void ShowChangeLogDirectory_WithValidRelativePath_SetsResolvedOverride()
    {
        var preferencesRepo = new FakeUserPreferencesRepository
        {
            Preferences = new UserPreferences { Language = "fr", LogFormat = LogFormat.Json }
        };

        var service = CreateService(
            preferencesRepo,
            new FakeLocalizationService(),
            out var pathProvider,
            out var menuService,
            out var messageService,
            out var inputService,
            out _);

        service.InitializeCulture();
        const string relativePath = "logs/custom-path";
        inputService.StringAnswers.Enqueue(relativePath);

        service.ConfigureParams(() => { });
        var settingsMenu = menuService.ShownMenuConfigs[0];
        settingsMenu.Actions[1]();

        string expected = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
        Assert.Equal(expected, pathProvider.LogDirectoryOverride);
        Assert.Equal(relativePath, preferencesRepo.Preferences.LogDirectory);
        Assert.Contains(messageService.Writes, call => call.Key == LocalizationKey.log_path_updated);
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
        var menuFactory = new MenuFactory();

        return new SettingsFlowService(
            preferencesRepo,
            preferencesRepo.Preferences,
            pathProvider,
            localization,
            menuService,
            menuFactory,
            messageService,
            inputService,
            consoleAdapter);
    }
}
