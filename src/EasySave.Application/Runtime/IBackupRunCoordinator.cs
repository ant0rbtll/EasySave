namespace EasySave.Application;

/// <summary>
/// Coordinates concurrent backup runs and prevents duplicate execution per job identifier.
/// </summary>
public interface IBackupRunCoordinator
{
    /// <summary>
    /// Executes a backup run while ensuring only one run per job id is active at a time.
    /// </summary>
    Task RunExclusiveAsync(int jobId, Func<CancellationToken, Task> run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the specified job is currently running.
    /// </summary>
    bool IsRunning(int jobId);
}
