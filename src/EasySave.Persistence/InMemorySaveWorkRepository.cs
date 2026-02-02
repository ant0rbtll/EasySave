using EasySave.Core;

namespace EasySave.Persistence;

public class InMemorySaveWorkRepository : ISaveWorkRepository
{
    private readonly int _maxJobs = 5;
    private readonly Dictionary<int, SaveWork> _jobs = new();
    private readonly IJobIdProvider _idProvider;

    public InMemorySaveWorkRepository(IJobIdProvider idProvider)
    {
        _idProvider = idProvider;
    }

    public void Add(SaveWork job)
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
        if (!_jobs.ContainsKey(id))
            throw new KeyNotFoundException($"Job with ID {id} not found.");

        _jobs.Remove(id);
    }

    public SaveWork GetById(int id)
    {
        if (!_jobs.ContainsKey(id))
            throw new KeyNotFoundException($"Job with ID {id} not found.");

        return _jobs[id];
    }

    public List<SaveWork> GetAll()
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
