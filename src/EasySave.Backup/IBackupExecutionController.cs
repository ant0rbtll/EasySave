namespace EasySave.Backup;

public enum BackupJobControlState
{
    Running,
    Paused,
    StopRequested
}

/// <summary>
/// Coordinates runtime control actions (pause/resume/stop) for backup execution.
/// </summary>
public interface IBackupExecutionController
{
    /// <summary>
    /// Initializes execution state for a backup job.
    /// </summary>
    void BeginJob(int jobId);

    /// <summary>
    /// Clears execution state for a backup job.
    /// </summary>
    void EndJob(int jobId);

    /// <summary>
    /// Requests pause for the currently running job.
    /// </summary>
    void Pause();

    /// <summary>
    /// Requests pause for a specific running job.
    /// </summary>
    void PauseForJob(int jobId);

    /// <summary>
    /// Resumes the currently paused job.
    /// </summary>
    void Resume();

    /// <summary>
    /// Resumes a specific paused job.
    /// </summary>
    void ResumeForJob(int jobId);

    /// <summary>
    /// Requests stop for the currently running job.
    /// </summary>
    void RequestStop();

    /// <summary>
    /// Requests stop for a specific running job.
    /// </summary>
    void RequestStopForJob(int jobId);

    /// <summary>
    /// Blocks execution while paused and throws when stop is requested.
    /// </summary>
    void WaitIfPausedOrThrowIfStopped();

    /// <summary>
    /// Dequeues a pending user action key to be logged.
    /// </summary>
    bool TryDequeueAction(out string actionKey);

    /// <summary>
    /// Gets the current runtime control state for a specific job when available.
    /// </summary>
    bool TryGetCurrentJobControlState(int jobId, out BackupJobControlState controlState);
}
