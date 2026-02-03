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
        public int ShowMenu(string[] menuItems, string menuLabel = "menu")
        {
            int index = 0;
            ConsoleKey key;

            do
            {
                DisplayLabel(menuLabel);

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
        /// Display a menu and execute the associated action
        /// </summary>
        public void ShowMenuWithActions(string[] menuItems, Dictionary<int, Action> menuActions, string menuLabel = "menu")
        {
            int selectedIndex = ShowMenu(menuItems, menuLabel);

            if (menuActions.TryGetValue(selectedIndex, out var action))
            {
                action();
            }
        }

        /// <summary>
        /// Display the title of a section
        /// </summary>
        public void DisplayLabel(string key)
        {
            Console.Write("====");
            string message = _localizationService.TranslateText(key);
            Console.Write(message);
            Console.Write("====\n");
        }

        /// <summary>
        /// Wait for user to press any key
        /// </summary>
        public bool WaitForUser(string messageKey = "waiting_user", ConsoleKey? key = null)
        {
            Console.WriteLine(_localizationService.TranslateText(messageKey));
            if (key == null)
            {
                Console.ReadKey(true);
                return true;
            } /*else {
               ConsoleKey? pressedKey = null;
                do
                {
                    pressedKey = Console.ReadKey(true);
                } while (pressedKey != key || pressedKey != ConsoleKey.N);
                return pressedKey == key;
            }*/
            return true;
        }
    }
}
