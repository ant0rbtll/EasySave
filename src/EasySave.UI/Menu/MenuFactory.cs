using System;
using System.Collections.Generic;
using System.Linq;
using EasySave.Localization;

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
            LocalizationKey[] items =
            {
                LocalizationKey.menu_create,
                LocalizationKey.menu_list,
                LocalizationKey.menu_save,
                LocalizationKey.menu_params,
                LocalizationKey.menu_quit
            };
            var actions = new Dictionary<int, Action>
        {
            { 0, _consoleUI.CreateBackupJob },
            { 1, _consoleUI.SeeSaveList },
            { 2, _consoleUI.SaveJob },
            { 3, _consoleUI.ConfigureParams },
            { 4, _consoleUI.Quit }
        };

            return new MenuConfig(items, actions, LocalizationKey.menu);
        }

        public MenuConfig CreateLocaleMenu()
        {
            LocalizationKey[] items = _consoleUI.LocalizationService.AllCultures
                .Values
                .Append(LocalizationKey.back)
                .ToArray();

            Dictionary<int, Action> actions = new()
            {
                { 0,  () => _consoleUI.ChangeLocale("fr")  },
                { 1,  () => _consoleUI.ChangeLocale("en")  },
                { 2, _consoleUI.ConfigureParams }
            };
            return new MenuConfig(items, actions, LocalizationKey.menu_params_locale);
        }

        public MenuConfig CreateParamsMenu()
        {
            LocalizationKey[] items = { LocalizationKey.menu_params_locale, LocalizationKey.back };
            Dictionary<int, Action> actions = new()
            {
                { 0, _consoleUI.ShowChangeLocale },
                { 1, _consoleUI.MainMenu }
            };

            return new MenuConfig(items, actions, LocalizationKey.menu_params);
        }
    }
}
