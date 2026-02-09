using EasySave.Localization;
using EasySave.UI.Services;

namespace EasySave.UI.Menu
{
    internal class MenuService : IMenuService
    {
        private readonly ILocalizationService _localizationService;
        private readonly IConsoleAdapter _consoleAdapter;

        public MenuService(ILocalizationService localizationService, IConsoleAdapter? consoleAdapter = null)
        {
            _localizationService = localizationService;
            _consoleAdapter = consoleAdapter ?? new SystemConsoleAdapter();
        }

        /// <summary>
        /// Display a menu and return the selected index
        /// </summary>
        public int ShowMenu(LocalizationKey[] menuItems, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null)
        {
            int index = 0;
            ConsoleKey key;
            int backIndex = Array.IndexOf(menuItems, LocalizationKey.back);

            do
            {
                _consoleAdapter.Clear();
                DisplayLabel(menuLabel);
                renderHeader?.Invoke();

                for (int i = 0; i < menuItems.Length; i++)
                {
                    string shortcutPrefix = i < 9 ? $"{i + 1}. " : "   ";
                    if (i == index)
                    {
                        _consoleAdapter.SetForegroundColor(ConsoleColor.Green);
                        _consoleAdapter.Write($"> {shortcutPrefix}");
                        _consoleAdapter.WriteLine(_localizationService.TranslateText(menuItems[i]));
                        _consoleAdapter.ResetColor();
                    }
                    else
                    {
                        _consoleAdapter.Write($"  {shortcutPrefix}");
                        _consoleAdapter.WriteLine(_localizationService.TranslateText(menuItems[i]));
                    }
                }

                key = _consoleAdapter.ReadKey(true).Key;

                if (TryGetShortcutSelection(key, menuItems.Length, out int selectedIndex))
                {
                    _consoleAdapter.Clear();
                    return selectedIndex;
                }

                if (key == ConsoleKey.Escape && backIndex >= 0)
                {
                    _consoleAdapter.Clear();
                    return backIndex;
                }
                else if (key == ConsoleKey.UpArrow && index > 0)
                {
                    index--;
                }
                else if (key == ConsoleKey.DownArrow && index < menuItems.Length - 1)
                {
                    index++;
                }

            } while (key != ConsoleKey.Enter);

            _consoleAdapter.Clear();
            return index;
        }

        /// <summary>
        /// Display a menu with string items and return the index of the selected item
        /// </summary>
        public int ShowMenu(string[] menuItems, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null)
        {
            int index = 0;
            ConsoleKey key;
            string backLabel = _localizationService.TranslateText(LocalizationKey.back);
            int backIndex = Array.FindIndex(
                menuItems,
                item => string.Equals(item, backLabel, StringComparison.OrdinalIgnoreCase));
            do
            {
                _consoleAdapter.Clear();
                DisplayLabel(menuLabel);
                renderHeader?.Invoke();

                for (int i = 0; i < menuItems.Length; i++)
                {
                    string shortcutPrefix = i < 9 ? $"{i + 1}. " : "   ";
                    if (i == index)
                    {
                        _consoleAdapter.SetForegroundColor(ConsoleColor.Green);
                        _consoleAdapter.Write($"> {shortcutPrefix}");
                        _consoleAdapter.WriteLine(menuItems[i]);
                        _consoleAdapter.ResetColor();
                    }
                    else
                    {
                        _consoleAdapter.Write($"  {shortcutPrefix}");
                        _consoleAdapter.WriteLine(menuItems[i]);
                    }
                }

                key = _consoleAdapter.ReadKey(true).Key;

                if (TryGetShortcutSelection(key, menuItems.Length, out int selectedIndex))
                {
                    _consoleAdapter.Clear();
                    return selectedIndex;
                }

                if (key == ConsoleKey.Escape && backIndex >= 0)
                {
                    _consoleAdapter.Clear();
                    return backIndex;
                }
                else if (key == ConsoleKey.UpArrow && index > 0)
                    index--;
                else if (key == ConsoleKey.DownArrow && index < menuItems.Length - 1)
                    index++;

            } while (key != ConsoleKey.Enter);

            _consoleAdapter.Clear();
            return index;
        }

        /// <summary>
        /// Display a menu and execute the associated action
        /// </summary>
        public void ShowMenuWithActions(LocalizationKey[] menuItems, Dictionary<int, Action> menuActions, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null)
        {
            int selectedIndex = ShowMenu(menuItems, menuLabel, renderHeader);

            if (menuActions.TryGetValue(selectedIndex, out var action))
            {
                action();
            }
        }

        /// <summary>
        /// Display a menu with string items and execute the associated action
        /// </summary>
        public void ShowMenuWithActions(string[] menuItems, Dictionary<int, Action> menuActions, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null)
        {
            int selectedIndex = ShowMenu(menuItems, menuLabel, renderHeader);

            if (menuActions.TryGetValue(selectedIndex, out var action))
            {
                action();
            }
        }

        /// <summary>
        /// Display a menu from MenuConfig and execute the associated action
        /// </summary>
        public void ShowMenuWithActions(MenuConfig menuConfig)
        {
            if (menuConfig.ItemsAsStrings != null)
            {
                ShowMenuWithActions(menuConfig.ItemsAsStrings, menuConfig.Actions, menuConfig.Label, menuConfig.RenderHeader);
            }
            else if (menuConfig.Items != null)
            {
                ShowMenuWithActions(menuConfig.Items, menuConfig.Actions, menuConfig.Label, menuConfig.RenderHeader);
            }
            else
            {
                throw new ArgumentException("MenuConfig must have either ItemsAsStrings or Items set.", nameof(menuConfig));
            }
        }

        /// <summary>
        /// Display the title of a section
        /// </summary>
        public void DisplayLabel(LocalizationKey key)
        {
            _consoleAdapter.Write("====");
            string message = _localizationService.TranslateText(key);
            _consoleAdapter.Write(message);
            _consoleAdapter.WriteLine("====");
        }

        /// <summary>
        /// Wait for user to press any key
        /// </summary>
        public void WaitForUser(LocalizationKey messageKey = LocalizationKey.waiting_user)
        {
            _consoleAdapter.WriteLine(_localizationService.TranslateText(messageKey));
            _consoleAdapter.ReadKey(true);
        }

        private static bool TryGetShortcutSelection(ConsoleKey key, int itemCount, out int selectedIndex)
        {
            selectedIndex = -1;
            int maxShortcutIndex = Math.Min(itemCount, 9);

            if (key >= ConsoleKey.D1 && key <= ConsoleKey.D9)
            {
                int numericIndex = (int)key - (int)ConsoleKey.D1;
                if (numericIndex < maxShortcutIndex)
                {
                    selectedIndex = numericIndex;
                    return true;
                }
            }

            if (key >= ConsoleKey.NumPad1 && key <= ConsoleKey.NumPad9)
            {
                int numericIndex = (int)key - (int)ConsoleKey.NumPad1;
                if (numericIndex < maxShortcutIndex)
                {
                    selectedIndex = numericIndex;
                    return true;
                }
            }

            return false;
        }
    }
}
