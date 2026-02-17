using EasySave.Core;
using EasySave.State;

namespace EasySave.Application;

/// <summary>
/// Enriches backup jobs with runtime metadata loaded from state persistence.
/// </summary>
public sealed class BackupJobStateService(IStateReader stateReader) : IBackupJobStateService
{
    private readonly IStateReader _stateReader = stateReader;

    /// <inheritdoc />
    public void ApplyState(BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        var entries = _stateReader.ReadEntries();
        ApplyState(job, entries);
    }

    /// <inheritdoc />
    public void ApplyState(IEnumerable<BackupJob> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        var entries = _stateReader.ReadEntries();
        foreach (var job in jobs)
        {
            if (job is not null)
            {
                ApplyState(job, entries);
            }
        }
    }

    /// <summary>
    /// Applies one state entry to one backup job model.
    /// </summary>
    /// <param name="job">Target job model to enrich.</param>
    /// <param name="entries">State entries indexed by backup job id.</param>
    private static void ApplyState(BackupJob job, IReadOnlyDictionary<int, StateEntry> entries)
    {
        if (entries.TryGetValue(job.Id, out var entry))
        {
            job.LastExecutionDate = entry.Timestamp;
            job.Status = MapStatus(entry.Status);
            job.IsActive = job.Status is BackupJobStatus.Active or BackupJobStatus.Paused;
            return;
        }

        job.LastExecutionDate = null;
        job.Status = BackupJobStatus.Inactive;
        job.IsActive = false;
    }

    private static BackupJobStatus MapStatus(BackupStatus status)
    {
        return status switch
        {
            BackupStatus.Inactive => BackupJobStatus.Inactive,
            BackupStatus.Active => BackupJobStatus.Active,
            BackupStatus.Done => BackupJobStatus.Done,
            BackupStatus.Error => BackupJobStatus.Error,
            BackupStatus.Paused => BackupJobStatus.Paused,
            _ => BackupJobStatus.Inactive
        };
    }
}
