using System.Text.Json;
using EasySave.Configuration;
using EasySave.Core;

namespace EasySave.Persistence;

/// <summary>
/// Implémentation du dépôt de jobs avec persistance JSON sur disque.
/// </summary>
public class JsonBackupJobRepository : IBackupJobRepository
{
    private readonly int _maxJobs = IBackupJobRepository.DefaultMaxJobs;
    private readonly IPathProvider _pathProvider;
    private readonly IJobIdProvider _idProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initialise une nouvelle instance du dépôt JSON.
    /// </summary>
    /// <param name="pathProvider">Fournisseur de chemins pour le fichier de configuration.</param>
    /// <param name="idProvider">Fournisseur d'identifiants pour les nouveaux jobs.</param>
    public JsonBackupJobRepository(IPathProvider pathProvider, IJobIdProvider idProvider)
    {
        _pathProvider = pathProvider;
        _idProvider = idProvider;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Levée si le nombre maximum de jobs est atteint ou si l'ID existe déjà.
    /// </exception>
    public void Add(BackupJob job)
    {
        var all = Load();

        if (all.Count >= _maxJobs)
            throw new InvalidOperationException($"Cannot add more than {_maxJobs} jobs.");

        if (job.Id == 0)
        {
            job.Id = _idProvider.NextId(all);
        }

        if (all.Any(j => j.Id == job.Id))
            throw new InvalidOperationException($"Job with ID {job.Id} already exists.");

        all.Add(job);
        Save(all);
    }

    /// <inheritdoc />
    /// <exception cref="KeyNotFoundException">Levée si le job n'existe pas.</exception>
    public void Remove(int id)
    {
        var all = Load();
        var job = all.FirstOrDefault(j => j.Id == id);

        if (job == null)
            throw new KeyNotFoundException($"Job with ID {id} not found.");

        all.Remove(job);
        Save(all);
    }

    /// <inheritdoc />
    /// <exception cref="KeyNotFoundException">Levée si le job n'existe pas.</exception>
    public BackupJob GetById(int id)
    {
        var all = Load();
        var job = all.FirstOrDefault(j => j.Id == id);

        if (job == null)
            throw new KeyNotFoundException($"Job with ID {id} not found.");

        return job;
    }

    /// <inheritdoc />
    public List<BackupJob> GetAll()
    {
        return Load();
    }

    /// <inheritdoc />
    public int Count()
    {
        return Load().Count;
    }

    /// <inheritdoc />
    public int MaxJobs()
    {
        return _maxJobs;
    }

    /// <summary>
    /// Charge les jobs depuis le fichier JSON.
    /// </summary>
    /// <returns>Liste des jobs, ou liste vide si le fichier n'existe pas ou est corrompu.</returns>
    private List<BackupJob> Load()
    {
        var path = _pathProvider.GetJobsConfigPath();

        if (!File.Exists(path))
            return new List<BackupJob>();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<BackupJob>>(json, _jsonOptions)
                   ?? new List<BackupJob>();
        }
        catch (JsonException)
        {
            return new List<BackupJob>();
        }
    }

    /// <summary>
    /// Sauvegarde les jobs dans le fichier JSON.
    /// </summary>
    /// <param name="all">Liste des jobs à sauvegarder.</param>
    private void Save(List<BackupJob> all)
    {
        var path = _pathProvider.GetJobsConfigPath();
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(all.OrderBy(j => j.Id).ToList(), _jsonOptions);
        File.WriteAllText(path, json);
    }
}
