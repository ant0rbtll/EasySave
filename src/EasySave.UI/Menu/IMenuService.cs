using EasySave.Localization;

namespace EasySave.UI.Menu;

/// <summary>
/// Defines menu rendering and interaction services for console navigation.
/// </summary>
internal interface IMenuService
{
    /// <summary>
    /// Displays a localized menu and returns the selected index.
    /// </summary>
    /// <param name="menuItems">Localized menu entries.</param>
    /// <param name="menuLabel">Localized menu title key.</param>
    /// <param name="renderHeader">Optional header renderer.</param>
    /// <returns>The selected item index.</returns>
    int ShowMenu(LocalizationKey[] menuItems, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null);

    /// <summary>
    /// Displays a plain text menu and returns the selected index.
    /// </summary>
    /// <param name="menuItems">Menu entries as plain text.</param>
    /// <param name="menuLabel">Localized menu title key.</param>
    /// <param name="renderHeader">Optional header renderer.</param>
    /// <returns>The selected item index.</returns>
    int ShowMenu(string[] menuItems, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null);

    /// <summary>
    /// Displays a localized menu and executes the selected action.
    /// </summary>
    /// <param name="menuItems">Localized menu entries.</param>
    /// <param name="menuActions">Actions mapped by selected index.</param>
    /// <param name="menuLabel">Localized menu title key.</param>
    /// <param name="renderHeader">Optional header renderer.</param>
    void ShowMenuWithActions(LocalizationKey[] menuItems, Dictionary<int, Action> menuActions, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null);

    /// <summary>
    /// Displays a plain text menu and executes the selected action.
    /// </summary>
    /// <param name="menuItems">Menu entries as plain text.</param>
    /// <param name="menuActions">Actions mapped by selected index.</param>
    /// <param name="menuLabel">Localized menu title key.</param>
    /// <param name="renderHeader">Optional header renderer.</param>
    void ShowMenuWithActions(string[] menuItems, Dictionary<int, Action> menuActions, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null);

    /// <summary>
    /// Displays a menu from a ready-to-use configuration and executes the selected action.
    /// </summary>
    /// <param name="menuConfig">Menu configuration containing items and actions.</param>
    void ShowMenuWithActions(MenuConfig menuConfig);

    /// <summary>
    /// Displays a localized section title.
    /// </summary>
    /// <param name="key">Localized title key.</param>
    void DisplayLabel(LocalizationKey key);

    /// <summary>
    /// Displays a prompt and waits for any user key.
    /// </summary>
    /// <param name="messageKey">Localized message key displayed before waiting.</param>
    void WaitForUser(LocalizationKey messageKey = LocalizationKey.waiting_user);
}
