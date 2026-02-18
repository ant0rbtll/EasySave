using EasySave.Core;

namespace EasySave.Application;

/// <summary>
/// Represents the lifecycle status of one backup run.
/// </summary>
public enum LogRunStatus
{
    /// <summary>
    /// The run started but has no closing terminal event yet.
    /// </summary>
    InProgress = 0,

    /// <summary>
    /// The run finished with an EndBackup event.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// The run ended with an error event.
    /// </summary>
    Error = 2,

    /// <summary>
    /// The run was stopped by the user.
    /// </summary>
    Stopped = 3,

    /// <summary>
    /// The run is currently paused by the user.
    /// </summary>
    Paused = 4,

    /// <summary>
    /// The run is currently blocked by business software detection.
    /// </summary>
    Blocked = 5
}

/// <summary>
/// Summarizes one backup job for a selected day.
/// </summary>
/// <param name="BackupId">Backup job identifier.</param>
/// <param name="BackupName">Backup job display name.</param>
/// <param name="RunCount">Number of runs found for this date.</param>
public sealed record LogJobSummary(
    int BackupId,
    string BackupName,
    int RunCount);

/// <summary>
/// Summarizes one execution run for a backup job.
/// </summary>
/// <param name="RunId">Stable identifier used to fetch run entries.</param>
/// <param name="BackupId">Backup job identifier.</param>
/// <param name="BackupName">Backup job display name.</param>
/// <param name="Format">Source log format containing this run.</param>
/// <param name="StartTimestamp">Run start timestamp.</param>
/// <param name="EndTimestamp">Run end timestamp when a terminal event is present.</param>
/// <param name="Status">Computed run status from terminal events.</param>
/// <param name="TotalDurationMs">Total duration from terminal event transfer time when available.</param>
/// <param name="TotalSizeBytes">Total backup size from EndBackup file size when available.</param>
public sealed record LogRunSummary(
    string RunId,
    int BackupId,
    string BackupName,
    LogFormat Format,
    DateTime StartTimestamp,
    DateTime? EndTimestamp,
    LogRunStatus Status,
    long? TotalDurationMs,
    long? TotalSizeBytes);
