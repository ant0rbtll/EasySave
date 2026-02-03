using System;
using System.Collections.Generic;
using System.Linq;
using EasySave.Localization;
using EasySave.Application;

namespace EasySave.UI.Menu
{
    internal class MenuFactory
    {
        private readonly ConsoleUI _consoleUI;
        private readonly BackupAppService _backupAppService;

        public MenuFactory(ConsoleUI consoleUI, BackupAppService backupAppService)
        {
            _consoleUI = consoleUI;
            _backupAppService = backupAppService;
        }

        public MenuConfig CreateMainMenu()
        {
            const int maxJobs = 5;
            int currentJobCount = _backupAppService.GetAllJobs().Count;
            bool hasJobs = currentJobCount > 0;
            bool canCreateJob = currentJobCount < maxJobs;

            var items = new List<LocalizationKey>();
            var actions = new Dictionary<int, Action>();
            int index = 0;

            if (canCreateJob)
            {
                items.Add(LocalizationKey.menu_create);
                actions[index++] = _consoleUI.CreateBackupJob;
            }

            if (hasJobs)
            {
                items.Add(LocalizationKey.menu_list);
                actions[index++] = _consoleUI.SeeSaveList;

                items.Add(LocalizationKey.menu_save);
                actions[index++] = _consoleUI.SaveJob;

                items.Add(LocalizationKey.menu_delete);
                actions[index++] = _consoleUI.DeleteBackupJob;
            }

            items.Add(LocalizationKey.menu_params);
            actions[index++] = _consoleUI.ConfigureParams;

            items.Add(LocalizationKey.menu_quit);
            actions[index] = _consoleUI.Quit;

            return new MenuConfig(items.ToArray(), actions, LocalizationKey.menu);
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
