using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.UI.Menu
{
    internal class MenuFactory
    {
        private readonly ConsoleUI _consoleUI;

        public MenuFactory(ConsoleUI consoleUI)
        {
            _consoleUI = consoleUI;
        }

        public MenuConfig CreateMainMenu()
        {
            string[] items = { "menu_create", "menu_list", "menu_save", "menu_params", "menu_quit" };
            var actions = new Dictionary<int, Action>
        {
            { 0, _consoleUI.CreateSaveWork },
            { 1, _consoleUI.SeeSaveList },
            { 2, _consoleUI.SaveJob },
            { 3, _consoleUI.ConfigureParams },
            { 4, _consoleUI.Quit }
        };

            return new MenuConfig(items, actions, "menu");
        }

        public MenuConfig CreateLocaleMenu()
        {
            string[] items = _consoleUI.LocalizationService.AllCultures
                .Values
                .Append("back")
                .ToArray();

            Dictionary<int, Action> actions = new()
            {
                { 0,  () => _consoleUI.ChangeLocale("fr")  },
                { 1,  () => _consoleUI.ChangeLocale("en")  },
                { 2, _consoleUI.ConfigureParams }
            };
            return new MenuConfig(items, actions, "menu_params_locale");
        }

        public MenuConfig CreateParamsMenu()
        {
            string[] items = { "menu_params_locale", "back" };
            Dictionary<int, Action> actions = new()
            {
                { 0, _consoleUI.ShowChangeLocale },
                { 1, _consoleUI.MainMenu }
            };

            return new MenuConfig(items, actions, "menu_params");
        }
    }
}
