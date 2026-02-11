using EasySave.Core;

namespace EasySave.Persistence;

/// <summary>
/// Repository for managing backup jobs.
/// </summary>
public interface IBackupJobRepository
{
    /// <summary>
    /// Adds a job to the repository.
    /// </summary>
    /// <param name="job">The job to add.</param>
    void Add(BackupJob job);

    /// <summary>
    /// Removes a job by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the job to remove.</param>
    void Remove(int id);

    /// <summary>
    /// Retrieves a job by its identifier.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>The matching job, or null if not found.</returns>
    BackupJob? GetById(int id);

    /// <summary>
    /// Retrieves all jobs.
    /// </summary>
    /// <returns>A list of all jobs.</returns>
    List<BackupJob> GetAll();

    /// <summary>
    /// Returns the number of jobs in the repository.
    /// </summary>
    /// <returns>The job count.</returns>
    int Count();

    /// <summary>
    /// Updates an existing job.
    /// </summary>
    /// <param name="job">The job with updated values.</param>
    void Update(BackupJob job);
}
