namespace EasySave.Backup;

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
    /// Resumes the currently paused job.
    /// </summary>
    void Resume();

    /// <summary>
    /// Requests stop for the currently running job.
    /// </summary>
    void RequestStop();

    /// <summary>
    /// Blocks execution while paused and throws when stop is requested.
    /// </summary>
    void WaitIfPausedOrThrowIfStopped();

    /// <summary>
    /// Dequeues a pending user action key to be logged.
    /// </summary>
    bool TryDequeueAction(out string actionKey);
}
