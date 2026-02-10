using EasySave.Core;

namespace EasySave.UI.Services;

/// <summary>
/// Tracks the in-memory edit state for a backup job update session.
/// </summary>
internal class JobEditSessionService
{
    private int? _editingJobId;
    private BackupJob? _editingJobSnapshot;

    /// <summary>
    /// Starts a job edit session or keeps the existing session when unchanged.
    /// </summary>
    /// <param name="job">Job currently being edited.</param>
    public void BeginOrRefresh(BackupJob job)
    {
        if (_editingJobId == job.Id && _editingJobSnapshot != null)
        {
            return;
        }

        _editingJobId = job.Id;
        _editingJobSnapshot = Clone(job);
    }

    /// <summary>
    /// Indicates whether the current in-memory job has unsaved changes.
    /// </summary>
    /// <param name="job">Job to compare with its original snapshot.</param>
    /// <returns><see langword="true"/> if changes are pending; otherwise <see langword="false"/>.</returns>
    public bool HasPendingChanges(BackupJob job)
    {
        if (_editingJobId != job.Id || _editingJobSnapshot == null)
        {
            return false;
        }

        return !AreEqual(job, _editingJobSnapshot);
    }

    /// <summary>
    /// Clears the current edit session state.
    /// </summary>
    public void Clear()
    {
        _editingJobId = null;
        _editingJobSnapshot = null;
    }

    /// <summary>
    /// Restores the job values from the captured snapshot.
    /// </summary>
    /// <param name="job">Job instance to restore.</param>
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

    /// <summary>
    /// Creates a detached copy of a backup job.
    /// </summary>
    /// <param name="job">Source job.</param>
    /// <returns>Cloned job instance.</returns>
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

    /// <summary>
    /// Compares two jobs for value equality.
    /// </summary>
    /// <param name="first">First job.</param>
    /// <param name="second">Second job.</param>
    /// <returns><see langword="true"/> if all persisted fields match; otherwise <see langword="false"/>.</returns>
    private static bool AreEqual(BackupJob first, BackupJob second)
    {
        return first.Id == second.Id
            && string.Equals(first.Name, second.Name, StringComparison.Ordinal)
            && string.Equals(first.Source, second.Source, StringComparison.Ordinal)
            && string.Equals(first.Destination, second.Destination, StringComparison.Ordinal)
            && first.Type == second.Type;
    }
}
