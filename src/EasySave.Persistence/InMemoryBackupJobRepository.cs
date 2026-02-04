using EasySave.Core;

namespace EasySave.Persistence;

/// <summary>
/// Implémentation en mémoire du dépôt de jobs de sauvegarde.
/// Les données ne sont pas persistées entre les exécutions.
/// </summary>
public class InMemoryBackupJobRepository : IBackupJobRepository
{
    private readonly int _maxJobs = IBackupJobRepository.DefaultMaxJobs;
    private readonly Dictionary<int, BackupJob> _jobs = new();
    private readonly IJobIdProvider _idProvider;

    /// <summary>
    /// Initialise une nouvelle instance du dépôt en mémoire.
    /// </summary>
    /// <param name="idProvider">Fournisseur d'identifiants pour les nouveaux jobs.</param>
    public InMemoryBackupJobRepository(IJobIdProvider idProvider)
    {
        _idProvider = idProvider;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Levée si le nombre maximum de jobs est atteint ou si l'ID existe déjà.
    /// </exception>
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

    /// <inheritdoc />
    /// <exception cref="KeyNotFoundException">Levée si le job n'existe pas.</exception>
    public void Remove(int id)
    {
        if (!_jobs.Remove(id))
            throw new KeyNotFoundException($"Job with ID {id} not found.");
    }

    /// <inheritdoc />
    /// <exception cref="KeyNotFoundException">Levée si le job n'existe pas.</exception>
    public BackupJob GetById(int id)
    {
        if (!_jobs.TryGetValue(id, out var job))
            throw new KeyNotFoundException($"Job with ID {id} not found.");

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
    /// <exception cref="KeyNotFoundException">Levée si le job n'existe pas.</exception>
    public void Update(BackupJob job)
    {
        if (!_jobs.ContainsKey(job.Id))
            throw new KeyNotFoundException($"Job with ID {job.Id} not found.");

        _jobs[job.Id] = job;
    }
}
