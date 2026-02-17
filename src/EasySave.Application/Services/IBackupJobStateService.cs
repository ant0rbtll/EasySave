using EasySave.Core;

namespace EasySave.Application.Services;

/// <summary>
/// Applies runtime state information to backup job models.
/// </summary>
public interface IBackupJobStateService
{
    /// <summary>
    /// Applies runtime state fields to one job.
    /// </summary>
    void ApplyState(BackupJob job);

    /// <summary>
    /// Applies runtime state fields to many jobs.
    /// </summary>
    void ApplyState(IEnumerable<BackupJob> jobs);
}
