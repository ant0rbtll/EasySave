using EasySave.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                Console.Clear();
                DisplayLabel(menuLabel);

                for (int i = 0; i < menuItems.Length; i++)
                {
                    if (i == index)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("> ");
                        Console.WriteLine(_localizationService.TranslateTexte(menuItems[i]));
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write("  ");
                        Console.WriteLine(_localizationService.TranslateTexte(menuItems[i]));
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
            string message = _localizationService.TranslateTexte(key);
            Console.Write(message);
            Console.Write("====\n");
        }

        /// <summary>
        /// Wait for user to press any key
        /// </summary>
        public void WaitForUser(string messageKey = "waiting_user")
        {
            Console.WriteLine(_localizationService.TranslateTexte(messageKey));
            Console.ReadKey(true);
        }
    }
}
