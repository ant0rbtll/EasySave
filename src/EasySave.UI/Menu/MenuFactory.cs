using EasySave.Core;
using EasySave.Localization;

namespace EasySave.UI.Menu
{
    /// <summary>
    /// Creates menu configurations from provided data and callbacks.
    /// </summary>
    internal class MenuFactory : IMenuFactory
    {
        /// <inheritdoc />
        public MenuConfig CreateMainMenu(int currentJobCount, Action onCreateJob, Action onManageJobs, Action onConfigureParams, Action onQuit)
        {
            const int maxJobs = 5;
            bool hasJobs = currentJobCount > 0;
            bool canCreateJob = currentJobCount < maxJobs;

            var items = new List<LocalizationKey>();
            var actions = new Dictionary<int, Action>();
            int index = 0;

            if (canCreateJob)
            {
                items.Add(LocalizationKey.menu_create);
                actions[index++] = onCreateJob;
            }

            if (hasJobs)
            {
                items.Add(LocalizationKey.menu_manage_jobs);
                actions[index++] = onManageJobs;
            }

            items.Add(LocalizationKey.menu_params);
            actions[index++] = onConfigureParams;

            items.Add(LocalizationKey.menu_quit);
            actions[index] = onQuit;

            return new MenuConfig(items.ToArray(), actions, LocalizationKey.menu);
        }

        /// <inheritdoc />
        public MenuConfig CreateLocaleMenu(
            IReadOnlyDictionary<string, LocalizationKey> cultures,
            Action<string> onSelectLocale,
            Action onBack,
            Action? renderHeader = null)
        {
            var items = new List<LocalizationKey>();
            var actions = new Dictionary<int, Action>();
            int index = 0;

            foreach (var culture in cultures)
            {
                items.Add(culture.Value);
                string locale = culture.Key;
                actions[index++] = () => onSelectLocale(locale);
            }

            items.Add(LocalizationKey.back);
            actions[index] = onBack;

            return new MenuConfig(items.ToArray(), actions, LocalizationKey.menu_params_locale, renderHeader);
        }

        /// <inheritdoc />
        public MenuConfig CreateParamsMenu(
            Action onShowChangeLocale,
            Action onShowChangeLogDirectory,
            Action onShowChangeLogFormat,
            Action onBack,
            Action? renderHeader = null)
        {
            LocalizationKey[] items =
            {
                LocalizationKey.menu_params_locale,
                LocalizationKey.menu_params_log_path,
                LocalizationKey.menu_params_log_format,
                LocalizationKey.back
            };

            Dictionary<int, Action> actions = new()
            {
                { 0, onShowChangeLocale },
                { 1, onShowChangeLogDirectory },
                { 2, onShowChangeLogFormat },
                { 3, onBack }
            };

            return new MenuConfig(items, actions, LocalizationKey.menu_params, renderHeader);
        }

        /// <inheritdoc />
        public MenuConfig CreateLogFormatMenu(
            string jsonLabel,
            string xmlLabel,
            string backLabel,
            Action onJson,
            Action onXml,
            Action onBack,
            Action? renderHeader = null)
        {
            string[] items =
            {
                jsonLabel,
                xmlLabel,
                backLabel
            };

            Dictionary<int, Action> actions = new()
            {
                { 0, onJson },
                { 1, onXml },
                { 2, onBack }
            };

            return new MenuConfig(items, actions, LocalizationKey.menu_params_log_format, renderHeader);
        }

        /// <inheritdoc />
        public MenuConfig CreateJobsListMenu(IEnumerable<BackupJob> jobs, string backLabel, Action<BackupJob> onSelectJob, Action onBack)
        {
            var items = new List<string>();
            var actions = new Dictionary<int, Action>();
            int index = 0;

            foreach (var job in jobs)
            {
                items.Add($"{job.Id} - {job.Name}");
                var capturedJob = job;
                actions[index++] = () => onSelectJob(capturedJob);
            }

            items.Add(backLabel);
            actions[index] = onBack;

            return new MenuConfig(items.ToArray(), actions, LocalizationKey.menu_manage_jobs);
        }

        /// <inheritdoc />
        public MenuConfig CreateJobDetailsMenu(
            BackupJob job,
            Action<BackupJob> onRunJob,
            Action<BackupJob> onUpdateJob,
            Action<BackupJob> onDeleteJob,
            Action onBack,
            Action? renderHeader = null)
        {
            LocalizationKey[] items =
            {
                LocalizationKey.menu_job_run,
                LocalizationKey.menu_job_update,
                LocalizationKey.menu_job_delete,
                LocalizationKey.back
            };

            Dictionary<int, Action> actions = new()
            {
                { 0, () => onRunJob(job) },
                { 1, () => onUpdateJob(job) },
                { 2, () => onDeleteJob(job) },
                { 3, onBack }
            };

            return new MenuConfig(items, actions, LocalizationKey.menu_job_details, renderHeader);
        }

        /// <inheritdoc />
        public MenuConfig CreateJobUpdateMenu(
            BackupJob job,
            Action<BackupJob, string> onUpdateField,
            Action<BackupJob> onSave,
            Action<BackupJob> onBack)
        {
            LocalizationKey[] items =
            {
                LocalizationKey.menu_job_update_name,
                LocalizationKey.menu_job_update_source,
                LocalizationKey.menu_job_update_destination,
                LocalizationKey.menu_job_update_type,
                LocalizationKey.menu_job_update_save,
                LocalizationKey.back
            };

            Dictionary<int, Action> actions = new()
            {
                { 0, () => onUpdateField(job, "name") },
                { 1, () => onUpdateField(job, "source") },
                { 2, () => onUpdateField(job, "destination") },
                { 3, () => onUpdateField(job, "type") },
                { 4, () => onSave(job) },
                { 5, () => onBack(job) }
            };

            return new MenuConfig(items, actions, LocalizationKey.menu_job_update);
        }
    }
}
