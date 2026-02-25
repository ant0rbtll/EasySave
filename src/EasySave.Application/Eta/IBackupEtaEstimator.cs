using EasySave.State;

namespace EasySave.Application;

/// <summary>
/// Computes an estimated remaining duration for active backup jobs using runtime snapshots.
/// </summary>
public interface IBackupEtaEstimator
{
    /// <summary>
    /// Updates the estimate for a specific job and returns the current ETA snapshot.
    /// </summary>
    /// <param name="jobId">Backup job identifier.</param>
    /// <param name="status">Current runtime status.</param>
    /// <param name="totalFiles">Total files planned for the backup job.</param>
    /// <param name="remainingFiles">Remaining files not yet completed.</param>
    /// <param name="totalSizeBytes">Total planned bytes for the backup job.</param>
    /// <param name="remainingSizeBytes">Remaining bytes to process.</param>
    /// <param name="observedAtUtc">UTC timestamp at which this snapshot was observed.</param>
    BackupEtaSnapshot UpdateEstimate(
        int jobId,
        BackupStatus status,
        int totalFiles,
        int remainingFiles,
        long totalSizeBytes,
        long remainingSizeBytes,
        DateTime observedAtUtc);

    /// <summary>
    /// Removes internal trackers for jobs that are no longer active.
    /// </summary>
    void Prune(IReadOnlyCollection<int> activeJobIds);
}
