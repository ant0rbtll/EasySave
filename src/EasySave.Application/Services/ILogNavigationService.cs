using EasySave.Core;
using EasySave.Log;

namespace EasySave.Application.Services;

/// <summary>
/// Provides hierarchical navigation over daily logs (date -> backup job -> run -> entries).
/// </summary>
public interface ILogNavigationService
{
    /// <summary>
    /// Lists backup jobs present in the logs for a given date.
    /// </summary>
    /// <param name="date">Target date.</param>
    /// <returns>Jobs sorted by name then backup id.</returns>
    IReadOnlyList<LogJobSummary> GetJobsByDate(DateOnly date);

    /// <summary>
    /// Lists execution runs for a given backup job and date.
    /// </summary>
    /// <param name="date">Target date.</param>
    /// <param name="backupId">Backup job identifier.</param>
    /// <returns>Runs sorted by start timestamp descending.</returns>
    IReadOnlyList<LogRunSummary> GetRunsByDateAndBackupId(DateOnly date, int backupId);

    /// <summary>
    /// Reads all log entries belonging to one run.
    /// </summary>
    /// <param name="date">Target date.</param>
    /// <param name="runId">Run identifier returned by <see cref="GetRunsByDateAndBackupId"/>.</param>
    /// <returns>Run entries sorted by timestamp descending.</returns>
    IReadOnlyList<LogEntry> GetEntriesByRun(DateOnly date, string runId);
}
