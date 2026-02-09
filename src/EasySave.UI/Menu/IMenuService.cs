using EasySave.Localization;

namespace EasySave.UI.Menu;

internal interface IMenuService
{
    int ShowMenu(LocalizationKey[] menuItems, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null);
    int ShowMenu(string[] menuItems, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null);
    void ShowMenuWithActions(LocalizationKey[] menuItems, Dictionary<int, Action> menuActions, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null);
    void ShowMenuWithActions(string[] menuItems, Dictionary<int, Action> menuActions, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null);
    void ShowMenuWithActions(MenuConfig menuConfig);
    void DisplayLabel(LocalizationKey key);
    void WaitForUser(LocalizationKey messageKey = LocalizationKey.waiting_user);
}
