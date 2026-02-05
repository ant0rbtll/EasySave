namespace EasySave.Configuration
{
    /// <summary>
    /// Provides file paths for application configuration, logs, and state.
    /// </summary>
    public interface IPathProvider
    {
        /// <summary>
        /// Gets the path to the daily log file for the specified date.
        /// </summary>
        /// <param name="date">The date for the log file.</param>
        /// <returns>The full path to the daily log file.</returns>
        string GetDailyLogPath(DateTime date);

        /// <summary>
        /// Gets the path to the real-time state file.
        /// </summary>
        /// <returns>The full path to the state file.</returns>
        string GetStatePath();

        /// <summary>
        /// Gets the path to the backup jobs configuration file.
        /// </summary>
        /// <returns>The full path to the jobs configuration file.</returns>
        string GetJobsConfigPath();

        /// <summary>
        /// Gets the path to the user preferences file.
        /// </summary>
        /// <returns>The full path to the user preferences file.</returns>
        string GetUserPreferencesPath();

        /// <summary>
        /// Sets a custom directory override for log file storage.
        /// </summary>
        /// <param name="directory">The custom directory path, or null to reset to default.</param>
        void SetLogDirectoryOverride(string? directory);
    }
}
