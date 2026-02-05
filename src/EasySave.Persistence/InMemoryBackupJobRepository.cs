using EasySave.Core;

namespace EasySave.Persistence;

/// <summary>
/// In-memory implementation of the backup job repository.
/// Data is not persisted between executions.
/// </summary>
public class InMemoryBackupJobRepository : IBackupJobRepository
{
    private readonly int _maxJobs = IBackupJobRepository.DefaultMaxJobs;
    private readonly Dictionary<int, BackupJob> _jobs = new();
    private readonly IJobIdProvider _idProvider;

    /// <summary>
    /// Initializes a new instance of the in-memory repository.
    /// </summary>
    /// <param name="idProvider">Identifier provider for new jobs.</param>
    public InMemoryBackupJobRepository(IJobIdProvider idProvider)
    {
        _idProvider = idProvider;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown if the maximum number of jobs is reached or if the ID already exists.
    /// </exception>
    public void Add(BackupJob job)
    {
        if (Count() >= _maxJobs) 
        {
            var e = new InvalidOperationException($"Cannot add more than {_maxJobs} jobs.");
            e.Data["errorKey"] = "error_add_max";
            e.Data["max_jobs"] = _maxJobs;
            throw e;
        }

        if (job.Id == 0)
        {
            job.Id = _idProvider.NextId(GetAll());
        }

        if (_jobs.ContainsKey(job.Id))
        {
            var e = new InvalidOperationException($"Job with ID {job.Id} already exists.");
            e.Data["errorKey"] = "error_add_exists";
            e.Data["job_id"] = job.Id;
            throw e;
        }
        _jobs[job.Id] = job;
    }

    /// <inheritdoc />
    /// <exception cref="KeyNotFoundException">Thrown if the job does not exist.</exception>
    public void Remove(int id)
    {
        if (!_jobs.Remove(id))
        {
            var e = new KeyNotFoundException($"Job with ID {id} not found");
            e.Data["errorKey"] = "error_job_not_found";
            e.Data["job_id"] = id;
            throw e;
        }
    }

    /// <inheritdoc />
    /// <exception cref="KeyNotFoundException">Thrown if the job does not exist.</exception>
    public BackupJob GetById(int id)
    {
        if (!_jobs.TryGetValue(id, out var job))
        {
            var e = new KeyNotFoundException($"Job with ID {id} not found");
            e.Data["errorKey"] = "error_job_not_found";
            e.Data["job_id"] = id;
            throw e;
        }
        return job;
    }

    /// <inheritdoc />
    public List<BackupJob> GetAll()
    {
        return _jobs.Values.OrderBy(j => j.Id).ToList();
    }

    /// <inheritdoc />
    public int Count()
    {
        return _jobs.Count;
    }

    /// <inheritdoc />
    public int MaxJobs()
    {
        return _maxJobs;
    }

    /// <inheritdoc />
    /// <exception cref="KeyNotFoundException">Thrown if the job does not exist.</exception>
    public void Update(BackupJob job)
    {
        if (!_jobs.ContainsKey(job.Id))
        {
            var e = new KeyNotFoundException($"Job with ID {job.Id} not found");
            e.Data["errorKey"] = "error_job_not_found";
            e.Data["job_id"] = job.Id;
            throw e;
        }
        _jobs[job.Id] = job;
    }
}
