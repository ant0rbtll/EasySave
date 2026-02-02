using EasySave.Core;

namespace EasySave.Persistence;

public class InMemoryBackupJobRepository : IBackupJobRepository
{
    private readonly int _maxJobs = IBackupJobRepository.DefaultMaxJobs;
    private readonly Dictionary<int, BackupJob> _jobs = new();
    private readonly IJobIdProvider _idProvider;

    public InMemoryBackupJobRepository(IJobIdProvider idProvider)
    {
        _idProvider = idProvider;
    }

    public void Add(BackupJob job)
    {
        if (Count() >= _maxJobs)
            throw new InvalidOperationException($"Cannot add more than {_maxJobs} jobs.");

        if (job.Id == 0)
        {
            job.Id = _idProvider.NextId(GetAll());
        }

        if (_jobs.ContainsKey(job.Id))
            throw new InvalidOperationException($"Job with ID {job.Id} already exists.");

        _jobs[job.Id] = job;
    }

    public void Remove(int id)
    {
        if (!_jobs.Remove(id))
            throw new KeyNotFoundException($"Job with ID {id} not found.");
    }

    public BackupJob GetById(int id)
    {
        if (!_jobs.TryGetValue(id, out var job))
            throw new KeyNotFoundException($"Job with ID {id} not found.");

        return job;
    }

    public List<BackupJob> GetAll()
    {
        return _jobs.Values.OrderBy(j => j.Id).ToList();
    }

    public int Count()
    {
        return _jobs.Count;
    }

    public int MaxJobs()
    {
        return _maxJobs;
    }
}
