using System;
using System.Collections.Generic;
using System.Linq;
using EasySave.Localization;
using EasySave.Application;

namespace EasySave.UI.Menu
{
    /// <summary>
    /// Creates menu configurations for the various application screens.
    /// </summary>
    internal class MenuFactory(ConsoleUI consoleUI, BackupApplicationService backupApplicationService)
    {
        private readonly ConsoleUI _consoleUI = consoleUI;
        private readonly BackupApplicationService _backupApplicationService = backupApplicationService;
        //private readonly BackupApplicationService backupApplicationService;

        public MenuConfig CreateMainMenu()
        {
            const int maxJobs = 5;
            int currentJobCount = _backupApplicationService.GetAllJobs().Count;
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
                items.Add(LocalizationKey.menu_manage_jobs);
                actions[index++] = _consoleUI.ShowJobsList;
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
            LocalizationKey[] items =
            {
                LocalizationKey.menu_params_locale,
                LocalizationKey.menu_params_log_path,
                LocalizationKey.back
            };
            Dictionary<int, Action> actions = new()
            {
                { 0, _consoleUI.ShowChangeLocale },
                { 1, _consoleUI.ShowChangeLogDirectory },
                { 2, _consoleUI.MainMenu }
            };

            return new MenuConfig(items, actions, LocalizationKey.menu_params);
        }

        public MenuConfig CreateJobsListMenu()
        {
            var jobs = _backupApplicationService.GetAllJobs();
            var items = new List<string>();
            var actions = new Dictionary<int, Action>();

            int index = 0;
            foreach (var job in jobs)
            {
                items.Add($"{job.Id} - {job.Name}");
                var capturedJob = job;
                actions[index++] = () => _consoleUI.ShowJobDetails(capturedJob);
            }

            items.Add(_consoleUI.LocalizationService.TranslateText(LocalizationKey.back));
            actions[index] = _consoleUI.MainMenu;

            return new MenuConfig(items.ToArray(), actions, LocalizationKey.menu_manage_jobs);
        }

        public MenuConfig CreateJobDetailsMenu(Core.BackupJob job, Action? renderHeader = null)
        {
            LocalizationKey[] items = { 
                LocalizationKey.menu_job_run,
                LocalizationKey.menu_job_update, 
                LocalizationKey.menu_job_delete, 
                LocalizationKey.back 
            };
            
            Dictionary<int, Action> actions = new()
            {
                { 0, () => _consoleUI.RunJob(job) },
                { 1, () => _consoleUI.UpdateJob(job) },
                { 2, () => _consoleUI.DeleteJob(job) },
                { 3, () => _consoleUI.ShowJobsList() }
            };

            return new MenuConfig(items, actions, LocalizationKey.menu_job_details, renderHeader);
        }

        public MenuConfig CreateJobUpdateMenu(Core.BackupJob job)
        {
            LocalizationKey[] items = { 
                LocalizationKey.menu_job_update_name,
                LocalizationKey.menu_job_update_source, 
                LocalizationKey.menu_job_update_destination, 
                LocalizationKey.menu_job_update_type,
                LocalizationKey.menu_job_update_save,
                LocalizationKey.back 
            };
            
            Dictionary<int, Action> actions = new()
            {
                { 0, () => _consoleUI.UpdateJobField(job, "name") },
                { 1, () => _consoleUI.UpdateJobField(job, "source") },
                { 2, () => _consoleUI.UpdateJobField(job, "destination") },
                { 3, () => _consoleUI.UpdateJobField(job, "type") },
                { 4, () => _consoleUI.SaveJobUpdate(job) },
                { 5, () => _consoleUI.ShowJobDetails(job) }
            };

            return new MenuConfig(items, actions, LocalizationKey.menu_job_update);
        }
    }
}
