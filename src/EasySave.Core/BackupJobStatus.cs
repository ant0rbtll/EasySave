namespace EasySave.Core;

/// <summary>
/// Represents the runtime status of a backup job.
/// </summary>
public enum BackupJobStatus
{
    /// <summary>
    /// No state information found for the job.
    /// </summary>
    Inactive,

    /// <summary>
    /// The backup is currently running.
    /// </summary>
    Active,

    /// <summary>
    /// The backup is waiting for global priority files to complete.
    /// </summary>
    Waiting,

    /// <summary>
    /// The last backup execution completed successfully.
    /// </summary>
    Done,

    /// <summary>
    /// The last backup execution ended with an error.
    /// </summary>
    Error,

    /// <summary>
    /// The backup is currently paused.
    /// </summary>
    Paused,

    /// <summary>
    /// The backup is temporarily blocked by business software detection.
    /// </summary>
    Blocked
}
