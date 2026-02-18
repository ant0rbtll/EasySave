using EasySave.Core;

namespace EasySave.Backup;

/// <summary>
/// Defines the contract for executing backup operations.
/// </summary>
public interface IBackupEngine
{
    /// <summary>
    /// Executes the specified backup job asynchronously.
    /// </summary>
    /// <param name="job">The backup job to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="executionContext">Runtime execution options.</param>
    Task Execute(
        BackupJob job,
        CancellationToken cancellationToken = default,
        BackupExecutionContext? executionContext = null);
}
