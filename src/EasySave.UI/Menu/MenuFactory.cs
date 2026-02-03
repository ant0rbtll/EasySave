using EasySave.Core;
using System;
using System.Collections.Generic;
using System.Linq;

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
            string[] items = { "menu_create", "menu_list", "menu_see" ,"menu_save", "menu_params", "menu_quit" };
            var actions = new Dictionary<int, Action>
        {
            { 0, _consoleUI.CreateBackupJob },
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

        
        public MenuConfig createSaveListMenu(List<BackupJob> backupJobs)
        {
            List<string> items = new();
            Dictionary<int, Action> actions = new();

            int index = 0;
            for (int i = 0; i < backupJobs.Count; i++)
            {
                BackupJob job = backupJobs[i];
                items.Add(job.Id + " - " + job.Name);
                actions.Add(i, () => _consoleUI.SeeOneJob(job));
                index++;
            }
            items.Add("back");
            actions.Add(index, _consoleUI.MainMenu);

            return new MenuConfig(items.ToArray(), actions, "menu_savelist");
        }
        /*
        public MenuConfig CreateSeeJobMenu(BackupJob job)
        {
            string[] items = { "backupjob_update", "backupjob_delete", "backupjob_save", "back" };
            Dictionary<int, Action> actions = new()
            {
                { 0, _consoleUI.StartUpdateJob(job) },
                { 1, _consoleUI.DeleteJob(job.Id) },
                { 2, _consoleUI.SaveJob(job.Id) },
                { 3, _consoleUI.MainMenu }
            };


            return new MenuConfig(items, actions, "menu_backupjob_actions");
        }

        public MenuConfig CreateUpdateJobMenu(BackupJob job)
        {
            string[] items = { "backupjob_update_name", "backupjob_update_source", "backupjob_update_destination", "backupjob_update_type", "backupjob_update_save","back_unsave" };
            Dictionary<int, Action> actions = new()
            {
                { 0, _consoleUI.UpdateJob(job, "name") },
                { 1, _consoleUI.UpdateJob(job, "source") },
                { 2, _consoleUI.UpdateJob(job, "destination") },
                { 3, _consoleUI.UpdateJob(job, "type") },
                { 4, _consoleUI.UpdateJob(job, "save") },
                { 4, _consoleUI.CancelUpdate(job) }
            };

            return new MenuConfig(items, actions, "menu_backupJob_update");
        }
        */
    }
}
