using EasySave.Localization;

namespace EasySave.UI.Menu
{
    internal class MenuService
    {
        private readonly ILocalizationService _localizationService;

        public MenuService(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        /// <summary>
        /// Display a menu and return the selected index
        /// </summary>
        public int ShowMenu(LocalizationKey[] menuItems, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null)
        {
            int index = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();
                DisplayLabel(menuLabel);
                renderHeader?.Invoke();

                for (int i = 0; i < menuItems.Length; i++)
                {
                    if (i == index)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("> ");
                        Console.WriteLine(_localizationService.TranslateText(menuItems[i]));
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write("  ");
                        Console.WriteLine(_localizationService.TranslateText(menuItems[i]));
                    }
                }

                key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.UpArrow && index > 0)
                {
                    index--;
                }
                else if (key == ConsoleKey.DownArrow && index < menuItems.Length - 1)
                {
                    index++;
                }

            } while (key != ConsoleKey.Enter);

            Console.Clear();
            return index;
        }

        /// <summary>
        /// Display a menu with string items and return the index of the selected item
        /// </summary>
        public int ShowMenu(string[] menuItems, LocalizationKey menuLabel = LocalizationKey.menu, Action? renderHeader = null)
        {
            int index = 0;
            ConsoleKey key;
            do
            {
                Console.Clear();
                DisplayLabel(menuLabel);
                renderHeader?.Invoke();

                for (int i = 0; i < menuItems.Length; i++)
                {
                    if (i == index)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("> ");
                        Console.WriteLine(menuItems[i]);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write("  ");
                        Console.WriteLine(menuItems[i]);
                    }
                }

                key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.UpArrow && index > 0)
                    index--;
                else if (key == ConsoleKey.DownArrow && index < menuItems.Length - 1)
                    index++;

            } while (key != ConsoleKey.Enter);

            Console.Clear();
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
            Console.Write("====");
            string message = _localizationService.TranslateText(key);
            Console.Write(message);
            Console.Write("====\n");
        }

        /// <summary>
        /// Wait for user to press any key
        /// </summary>
        public void WaitForUser(LocalizationKey messageKey = LocalizationKey.waiting_user)
        {
            Console.WriteLine(_localizationService.TranslateText(messageKey));
            Console.ReadKey(true);
        }
    }
}
