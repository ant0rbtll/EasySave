using EasySave.Core;
using EasySave.Exceptions;

namespace EasySave.Persistence;

/// <summary>
/// In-memory implementation of the backup job repository.
/// Data is not persisted between executions.
/// </summary>
public class InMemoryBackupJobRepository : IBackupJobRepository
{
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
    /// <exception cref="JobAlreadyExistException">
    /// Thrown if the maximum number of jobs is reached or if the ID already exists.
    /// </exception>
    public void Add(BackupJob job)
    {
        if (job.Id == 0)
        {
            job.Id = _idProvider.NextId(GetAll());
        }

        if (_jobs.ContainsKey(job.Id))
        {
            throw new JobAlreadyExistException(job.Id);
        }
        _jobs[job.Id] = job;
    }

    /// <inheritdoc />
    /// <exception cref="JobNotFoundException">Thrown if the job does not exist.</exception>
    public void Remove(int id)
    {
        if (!_jobs.Remove(id))
        {
            throw new JobNotFoundException(id);
        }
    }

    /// <inheritdoc />
    /// <exception cref="JobNotFoundException">Thrown if the job does not exist.</exception>
    public BackupJob GetById(int id)
    {
        if (!_jobs.TryGetValue(id, out var job))
        {
            throw new JobNotFoundException(id);
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
    /// <exception cref="JobNotFoundException">Thrown if the job does not exist.</exception>
    public void Update(BackupJob job)
    {
        if (!_jobs.ContainsKey(job.Id))
        {
            throw new JobNotFoundException(job.Id);
        }
        _jobs[job.Id] = job;
    }
}
