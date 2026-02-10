namespace EasySave.UI.Tests;

public class MenuServiceAdditionalTests
{
    [Fact]
    public void Constructor_WithNullConsoleAdapter_UsesDefaultAdapter()
    {
        var localization = new FakeLocalizationService();

        var service = new MenuService(localization);

        Assert.NotNull(service);
    }

    [Fact]
    public void ShowMenu_WithArrowNavigationAndEnter_ReturnsCurrentIndex()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.DownArrow);
        console.EnqueueKey(ConsoleKey.DownArrow);
        console.EnqueueKey(ConsoleKey.Enter, '\n');

        var service = new MenuService(localization, console);
        var selected = service.ShowMenu(
            [LocalizationKey.menu_create, LocalizationKey.menu_manage_jobs, LocalizationKey.menu_params],
            LocalizationKey.menu);

        Assert.Equal(2, selected);
    }

    [Fact]
    public void ShowMenu_WithNumPadShortcut_ReturnsMappedIndex()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.NumPad2, '2');

        var service = new MenuService(localization, console);
        var selected = service.ShowMenu(
            [LocalizationKey.menu_create, LocalizationKey.menu_manage_jobs],
            LocalizationKey.menu);

        Assert.Equal(1, selected);
    }

    [Fact]
    public void ShowMenu_StringMenu_WithEscapeOnBack_ReturnsBackIndex()
    {
        var localization = new FakeLocalizationService();
        localization.KeyTranslations[LocalizationKey.back] = "Back";
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Escape);

        var service = new MenuService(localization, console);
        var selected = service.ShowMenu(["Item", "back"], LocalizationKey.menu);

        Assert.Equal(1, selected);
    }

    [Fact]
    public void ShowMenu_StringMenu_WithoutBack_DoesNotExitOnEscape()
    {
        var localization = new FakeLocalizationService();
        localization.KeyTranslations[LocalizationKey.back] = "Back";
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Escape);
        console.EnqueueKey(ConsoleKey.Enter, '\n');

        var service = new MenuService(localization, console);
        var selected = service.ShowMenu(["A", "B"], LocalizationKey.menu);

        Assert.Equal(0, selected);
    }

    [Fact]
    public void ShowMenuWithActions_String_ExecutesSelectedAction()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D2, '2');
        var service = new MenuService(localization, console);

        var called = false;
        service.ShowMenuWithActions(
            ["A", "B"],
            new Dictionary<int, Action>
            {
                [0] = () => { },
                [1] = () => called = true
            },
            LocalizationKey.menu);

        Assert.True(called);
    }

    [Fact]
    public void DisplayLabel_WritesFramedTranslatedMessage()
    {
        var localization = new FakeLocalizationService();
        localization.KeyTranslations[LocalizationKey.menu] = "Menu";
        var console = new FakeConsoleAdapter();
        var service = new MenuService(localization, console);

        service.DisplayLabel(LocalizationKey.menu);

        Assert.Equal("W:====", console.Events[0]);
        Assert.Equal("W:Menu", console.Events[1]);
        Assert.Equal("WL:====", console.Events[2]);
    }

    [Fact]
    public void ShowMenu_WithOutOfRangeShortcut_IgnoresShortcutAndKeepsSelection()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D9, '9');
        console.EnqueueKey(ConsoleKey.Enter, '\n');

        var service = new MenuService(localization, console);
        var selected = service.ShowMenu(
            [LocalizationKey.menu_create, LocalizationKey.menu_manage_jobs],
            LocalizationKey.menu);

        Assert.Equal(0, selected);
    }

    [Fact]
    public void ShowMenu_WithDownThenUp_ReturnsMovedBackSelection()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.DownArrow);
        console.EnqueueKey(ConsoleKey.UpArrow);
        console.EnqueueKey(ConsoleKey.Enter, '\n');

        var service = new MenuService(localization, console);
        var selected = service.ShowMenu(
            [LocalizationKey.menu_create, LocalizationKey.menu_manage_jobs],
            LocalizationKey.menu);

        Assert.Equal(0, selected);
    }

    [Fact]
    public void ShowMenuWithActions_Localized_WhenSelectionHasNoAction_DoesNothing()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D2, '2');
        var service = new MenuService(localization, console);

        var called = false;
        service.ShowMenuWithActions(
            [LocalizationKey.menu_create, LocalizationKey.menu_manage_jobs],
            new Dictionary<int, Action> { [0] = () => called = true });

        Assert.False(called);
    }

    [Fact]
    public void ShowMenuWithActions_String_WhenSelectionHasNoAction_DoesNothing()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D2, '2');
        var service = new MenuService(localization, console);

        var called = false;
        service.ShowMenuWithActions(
            ["A", "B"],
            new Dictionary<int, Action> { [0] = () => called = true });

        Assert.False(called);
    }

    [Fact]
    public void ShowMenuWithActions_MenuConfig_WithStringItems_ExecutesAction()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D1, '1');
        var service = new MenuService(localization, console);

        var called = false;
        var menuConfig = new MenuConfig(
            ["A", "B"],
            new Dictionary<int, Action>
            {
                [0] = () => called = true
            },
            LocalizationKey.menu);

        service.ShowMenuWithActions(menuConfig);

        Assert.True(called);
    }

    [Fact]
    public void ShowMenuWithActions_MenuConfig_WithLocalizedItems_ExecutesAction()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D1, '1');
        var service = new MenuService(localization, console);

        var called = false;
        var menuConfig = new MenuConfig(
            [LocalizationKey.menu_create, LocalizationKey.back],
            new Dictionary<int, Action>
            {
                [0] = () => called = true
            },
            LocalizationKey.menu);

        service.ShowMenuWithActions(menuConfig);

        Assert.True(called);
    }
}
