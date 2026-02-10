namespace EasySave.UI.Tests;

public class MenuServiceTests
{
    [Fact]
    public void ShowMenu_WithShortcut_ReturnsSelectedIndex()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D2, '2');

        var service = new MenuService(localization, console);

        var selected = service.ShowMenu(
            [LocalizationKey.menu_create, LocalizationKey.menu_manage_jobs],
            LocalizationKey.menu);

        Assert.Equal(1, selected);
    }

    [Fact]
    public void ShowMenu_WithEscape_ReturnsBackIndex()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Escape);

        var service = new MenuService(localization, console);

        var selected = service.ShowMenu(
            [LocalizationKey.menu_create, LocalizationKey.back],
            LocalizationKey.menu);

        Assert.Equal(1, selected);
    }

    [Fact]
    public void ShowMenuWithActions_ExecutesMappedAction()
    {
        var localization = new FakeLocalizationService();
        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.D1, '1');

        var service = new MenuService(localization, console);

        var wasCalled = false;
        service.ShowMenuWithActions(
            [LocalizationKey.menu_create],
            new Dictionary<int, Action>
            {
                { 0, () => wasCalled = true }
            });

        Assert.True(wasCalled);
    }

    [Fact]
    public void WaitForUser_WritesMessageAndReadsOneKey()
    {
        var localization = new FakeLocalizationService();
        localization.KeyTranslations[LocalizationKey.waiting_user] = "Press any key";

        var console = new FakeConsoleAdapter();
        console.EnqueueKey(ConsoleKey.Enter, '\n');

        var service = new MenuService(localization, console);

        service.WaitForUser();

        Assert.Contains("WL:Press any key", console.Events);
    }
}
