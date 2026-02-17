namespace EasySave.AppCommon.Services;

/// <summary>
/// Exposes runtime controls for the currently running backup execution.
/// </summary>
public interface IBackupExecutionControlService
{
    /// <summary>
    /// Requests pause for the active backup.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes the active paused backup.
    /// </summary>
    void Resume();

    /// <summary>
    /// Requests stop for the active backup.
    /// </summary>
    void Stop();

    /// <summary>
    /// Requests stop for all active backups.
    /// </summary>
    void StopAll();
}
