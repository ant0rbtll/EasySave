using EasySave.Core;

namespace EasySave.Backup;

/// <summary>
/// Defines the contract for executing backup operations.
/// </summary>
public interface IBackupEngine
{
    /// <summary>
    /// Executes the specified backup job.
    /// </summary>
    /// <param name="job">The backup job to execute.</param>
    void Execute(BackupJob job);
}
