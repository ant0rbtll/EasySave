using EasySave.Core;

namespace EasySave.UI.Services;

/// <summary>
/// Tracks the in-memory edit state for a backup job update session.
/// </summary>
internal class JobEditSessionService
{
    private int? _editingJobId;
    private BackupJob? _editingJobSnapshot;

    public void BeginOrRefresh(BackupJob job)
    {
        if (_editingJobId == job.Id && _editingJobSnapshot != null)
        {
            return;
        }

        _editingJobId = job.Id;
        _editingJobSnapshot = Clone(job);
    }

    public bool HasPendingChanges(BackupJob job)
    {
        if (_editingJobId != job.Id || _editingJobSnapshot == null)
        {
            return false;
        }

        return !AreEqual(job, _editingJobSnapshot);
    }

    public void Clear()
    {
        _editingJobId = null;
        _editingJobSnapshot = null;
    }

    public void Restore(BackupJob job)
    {
        if (_editingJobId != job.Id || _editingJobSnapshot == null)
        {
            return;
        }

        job.Name = _editingJobSnapshot.Name;
        job.Source = _editingJobSnapshot.Source;
        job.Destination = _editingJobSnapshot.Destination;
        job.Type = _editingJobSnapshot.Type;
    }

    private static BackupJob Clone(BackupJob job)
    {
        return new BackupJob
        {
            Id = job.Id,
            Name = job.Name,
            Source = job.Source,
            Destination = job.Destination,
            Type = job.Type
        };
    }

    private static bool AreEqual(BackupJob first, BackupJob second)
    {
        return first.Id == second.Id
            && string.Equals(first.Name, second.Name, StringComparison.Ordinal)
            && string.Equals(first.Source, second.Source, StringComparison.Ordinal)
            && string.Equals(first.Destination, second.Destination, StringComparison.Ordinal)
            && first.Type == second.Type;
    }
}
