namespace EasySave.State;

/// <summary>
/// Represents the current status of a backup operation.
/// </summary>
public enum BackupStatus
{
    /// <summary>
    /// The backup is not currently running.
    /// </summary>
    Inactive,

    /// <summary>
    /// The backup is currently in progress.
    /// </summary>
    Active,

    /// <summary>
    /// The backup completed successfully.
    /// </summary>
    Done,

    /// <summary>
    /// The backup encountered an error.
    /// </summary>
    Error
}
