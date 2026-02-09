using EasySave.Core;
using EasySave.Localization;

namespace EasySave.UI.Menu;

/// <summary>
/// Defines menu configuration builders used by the UI flows.
/// </summary>
internal interface IMenuFactory
{
    /// <summary>
    /// Builds the main menu with actions depending on current job count.
    /// </summary>
    /// <param name="currentJobCount">Current number of configured backup jobs.</param>
    /// <param name="onCreateJob">Action invoked when creating a new job.</param>
    /// <param name="onManageJobs">Action invoked when navigating to jobs management.</param>
    /// <param name="onConfigureParams">Action invoked when opening settings.</param>
    /// <param name="onQuit">Action invoked when quitting the application.</param>
    /// <returns>The configured main menu.</returns>
    MenuConfig CreateMainMenu(int currentJobCount, Action onCreateJob, Action onManageJobs, Action onConfigureParams, Action onQuit);

    /// <summary>
    /// Builds the locale selection menu.
    /// </summary>
    /// <param name="cultures">Supported cultures and their display keys.</param>
    /// <param name="onSelectLocale">Action invoked when a locale is selected.</param>
    /// <param name="onBack">Action invoked for the back entry.</param>
    /// <param name="renderHeader">Optional header renderer.</param>
    /// <returns>The configured locale menu.</returns>
    MenuConfig CreateLocaleMenu(
        IReadOnlyDictionary<string, LocalizationKey> cultures,
        Action<string> onSelectLocale,
        Action onBack,
        Action? renderHeader = null);

    /// <summary>
    /// Builds the settings root menu.
    /// </summary>
    /// <param name="onShowChangeLocale">Action invoked when opening locale settings.</param>
    /// <param name="onShowChangeLogDirectory">Action invoked when opening log path settings.</param>
    /// <param name="onShowChangeLogFormat">Action invoked when opening log format settings.</param>
    /// <param name="onBack">Action invoked for the back entry.</param>
    /// <param name="renderHeader">Optional header renderer.</param>
    /// <returns>The configured settings menu.</returns>
    MenuConfig CreateParamsMenu(
        Action onShowChangeLocale,
        Action onShowChangeLogDirectory,
        Action onShowChangeLogFormat,
        Action onBack,
        Action? renderHeader = null);

    /// <summary>
    /// Builds the log format selection menu.
    /// </summary>
    /// <param name="jsonLabel">Localized label for JSON format.</param>
    /// <param name="xmlLabel">Localized label for XML format.</param>
    /// <param name="backLabel">Localized label for back entry.</param>
    /// <param name="onJson">Action invoked when JSON is selected.</param>
    /// <param name="onXml">Action invoked when XML is selected.</param>
    /// <param name="onBack">Action invoked for the back entry.</param>
    /// <param name="renderHeader">Optional header renderer.</param>
    /// <returns>The configured log format menu.</returns>
    MenuConfig CreateLogFormatMenu(
        string jsonLabel,
        string xmlLabel,
        string backLabel,
        Action onJson,
        Action onXml,
        Action onBack,
        Action? renderHeader = null);

    /// <summary>
    /// Builds the menu listing all configured backup jobs.
    /// </summary>
    /// <param name="jobs">Jobs to render in the menu.</param>
    /// <param name="backLabel">Localized label for back entry.</param>
    /// <param name="onSelectJob">Action invoked when selecting one job.</param>
    /// <param name="onBack">Action invoked for the back entry.</param>
    /// <returns>The configured jobs list menu.</returns>
    MenuConfig CreateJobsListMenu(IEnumerable<BackupJob> jobs, string backLabel, Action<BackupJob> onSelectJob, Action onBack);

    /// <summary>
    /// Builds the menu for a single job details screen.
    /// </summary>
    /// <param name="job">Job bound to the menu actions.</param>
    /// <param name="onRunJob">Action invoked to run the job.</param>
    /// <param name="onUpdateJob">Action invoked to edit the job.</param>
    /// <param name="onDeleteJob">Action invoked to delete the job.</param>
    /// <param name="onBack">Action invoked for the back entry.</param>
    /// <param name="renderHeader">Optional header renderer.</param>
    /// <returns>The configured job details menu.</returns>
    MenuConfig CreateJobDetailsMenu(
        BackupJob job,
        Action<BackupJob> onRunJob,
        Action<BackupJob> onUpdateJob,
        Action<BackupJob> onDeleteJob,
        Action onBack,
        Action? renderHeader = null);

    /// <summary>
    /// Builds the menu used while editing one backup job.
    /// </summary>
    /// <param name="job">Job being edited.</param>
    /// <param name="onUpdateField">Action invoked to edit one job field.</param>
    /// <param name="onSave">Action invoked to persist updates.</param>
    /// <param name="onBack">Action invoked when exiting update mode.</param>
    /// <returns>The configured job update menu.</returns>
    MenuConfig CreateJobUpdateMenu(
        BackupJob job,
        Action<BackupJob, string> onUpdateField,
        Action<BackupJob> onSave,
        Action<BackupJob> onBack);
}
