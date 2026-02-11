using EasySave.Core;
using EasySave.State;

namespace EasySave.Application;

/// <summary>
/// Enriches backup jobs with runtime metadata loaded from state persistence.
/// </summary>
public sealed class BackupJobStateService(IStateReader stateReader) : IBackupJobStateService
{
    private readonly IStateReader _stateReader = stateReader;

    public void ApplyState(BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        var entries = _stateReader.ReadEntries();
        ApplyState(job, entries);
    }

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

    private static void ApplyState(BackupJob job, IReadOnlyDictionary<int, StateEntry> entries)
    {
        if (entries.TryGetValue(job.Id, out var entry))
        {
            job.LastExecutionDate = entry.Timestamp;
            job.IsActive = entry.Status == BackupStatus.Active;
            return;
        }

        job.LastExecutionDate = null;
        job.IsActive = false;
    }
}
