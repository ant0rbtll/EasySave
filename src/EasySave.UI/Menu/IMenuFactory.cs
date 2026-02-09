using EasySave.Core;
using EasySave.Localization;

namespace EasySave.UI.Menu;

internal interface IMenuFactory
{
    MenuConfig CreateMainMenu(int currentJobCount, Action onCreateJob, Action onManageJobs, Action onConfigureParams, Action onQuit);

    MenuConfig CreateLocaleMenu(
        IReadOnlyDictionary<string, LocalizationKey> cultures,
        Action<string> onSelectLocale,
        Action onBack,
        Action? renderHeader = null);

    MenuConfig CreateParamsMenu(
        Action onShowChangeLocale,
        Action onShowChangeLogDirectory,
        Action onShowChangeLogFormat,
        Action onBack,
        Action? renderHeader = null);

    MenuConfig CreateLogFormatMenu(
        string jsonLabel,
        string xmlLabel,
        string backLabel,
        Action onJson,
        Action onXml,
        Action onBack,
        Action? renderHeader = null);

    MenuConfig CreateJobsListMenu(IEnumerable<BackupJob> jobs, string backLabel, Action<BackupJob> onSelectJob, Action onBack);

    MenuConfig CreateJobDetailsMenu(
        BackupJob job,
        Action<BackupJob> onRunJob,
        Action<BackupJob> onUpdateJob,
        Action<BackupJob> onDeleteJob,
        Action onBack,
        Action? renderHeader = null);

    MenuConfig CreateJobUpdateMenu(
        BackupJob job,
        Action<BackupJob, string> onUpdateField,
        Action<BackupJob> onSave,
        Action<BackupJob> onBack);
}
