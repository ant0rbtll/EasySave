using EasySave.Persistence;
using EasySave.Backup;
using EasySave.Core;

namespace EasySave.Application;

/// <summary>
/// Initializes a new instance of the <see cref="BackupApplicationService"/> class.
/// </summary>
/// <param name="repo">The repository used for data persistence.</param>
/// <param name="backupEngine">The engine responsible for executing backup jobs.</param>
public class BackupApplicationService(IBackupJobRepository repo, IBackupEngine backupEngine)
{
    private readonly IBackupJobRepository _repo = repo;
    private readonly IBackupEngine _engine = backupEngine;

    /// <summary>
    /// Creates and saves a new backup job.
    /// </summary>
    /// <param name="name">Unique name of the job.</param>
    /// <param name="source">Source folder path.</param>
    /// <param name="destination">Destination folder path.</param>
    /// <param name="type">Type of backup (Full or Differential).</param>
    public void CreateJob(string name, string source, string destination, BackupType type)
    {
        var job = new BackupJob
        {
            Name = name,
            Source = source,
            Destination = destination,
            Type = type
        };

        _repo.Add(job);
    }

    /// <summary>
    /// Deletes an existing backup job using its unique identifier.
    /// </summary>
    /// <param name="id">Identifier of the job to remove.</param>
    public void RemoveJob(int id)
    {
        _repo.Remove(id);
    }

    /// <summary>
    /// Executes a specific backup job.
    /// </summary>
    /// <param name="job">The backup job to execute.</param>
    public void RunJob(BackupJob job)
    {
        _engine.Execute(job);
    }

    /// <summary>
    /// Executes a specific backup job by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the job to run.</param>
    public void RunJobById(int id)
    {
        var job = _repo.GetById(id);
        if (job != null) RunJob(job);
    }

    /// <summary>
    /// Executes a specific list of backup jobs.
    /// </summary>
    /// <param name="ids">Array of job identifiers to launch.</param>
    public void RunJobsByIds(int[] ids)
    {
        foreach (int id in ids)
        {
            var job = _repo.GetById(id);
            if (job != null) RunJob(job);
        }
    }

    /// <summary>
    /// Retrieves and executes all registered backup jobs.
    /// </summary>
    public void RunAllJobs()
    {
        var jobs = _repo.GetAll();
        foreach (var job in jobs)
        {
            RunJob(job);
        }
    }

    /// <summary>
    /// Retrieves all backup jobs from the repository.
    /// </summary>
    /// <returns>A list of <see cref="BackupJob"/> objects.</returns>
    public List<BackupJob> GetAllJobs()
    {
        return _repo.GetAll();
    }

    /// <summary>
    /// Retrieves a specific backup job by ID.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>The BackupJob if found, null otherwise.</returns>
    public BackupJob? GetJobById(int id)
    {
        return _repo.GetById(id);
    }

    /// <summary>
    /// Updates an existing backup job with new values.
    /// </summary>
    /// <param name="job">The backup job with updated values.</param>
    public void UpdateJob(BackupJob job)
    {
        _repo.Update(job);
    }
}