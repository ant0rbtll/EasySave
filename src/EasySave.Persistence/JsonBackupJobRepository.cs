using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Configuration;
using EasySave.Core;
using EasySave.Core.Exceptions;
using System.Text.Json;

namespace EasySave.Persistence;

/// <summary>
/// JSON-based repository implementation for backup job persistence.
/// </summary>
public class JsonBackupJobRepository : IBackupJobRepository
{
    private readonly IPathProvider _pathProvider;
    private readonly IJobIdProvider _idProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the JSON backup job repository.
    /// </summary>
    /// <param name="pathProvider">Path provider for the configuration file.</param>
    /// <param name="idProvider">ID provider for new jobs.</param>
    public JsonBackupJobRepository(IPathProvider pathProvider, IJobIdProvider idProvider)
    {
        _pathProvider = pathProvider;
        _idProvider = idProvider;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true)
            }
        };
    }

    /// <inheritdoc />
    /// <exception cref="JobAlreadyExistException">
    /// Thrown if the maximum number of jobs is reached or if the ID already exists.
    /// </exception>
    public void Add(BackupJob job)
    {
        var all = Load();

        if (job.Id == 0)
        {
            job.Id = _idProvider.NextId(all);
        }

        if (all.Any(j => j.Id == job.Id))
        {
            throw new JobAlreadyExistException(job.Id);
        }

        all.Add(job);
        Save(all);
    }

    /// <inheritdoc />
    /// <exception cref="JobNotFoundException">Thrown if the job does not exist.</exception>
    public void Remove(int id)
    {
        var all = Load();

        var job = all.FirstOrDefault(j => j.Id == id);

        if (job == null)
        {
            throw new JobNotFoundException(id);
        }

        all.Remove(job);
        Save(all);
    }

    /// <inheritdoc />
    public BackupJob? GetById(int id)
    {
        var all = Load();
        return all.FirstOrDefault(j => j.Id == id);
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
    /// <exception cref="JobNotFoundException">Thrown if the job does not exist.</exception>
    public void Update(BackupJob job)
    {
        var all = Load();

        var existing = all.FirstOrDefault(j => j.Id == job.Id);

        if (existing == null)
        {
            throw new JobNotFoundException(job.Id);
        }

        existing.Name = job.Name;
        existing.Source = job.Source;
        existing.Destination = job.Destination;
        existing.Type = job.Type;

        Save(all);
    }

    /// <summary>
    /// Loads jobs from the JSON file.
    /// </summary>
    /// <returns>List of jobs, or empty list if file does not exist or is corrupted.</returns>
    private List<BackupJob> Load()
    {
        var path = _pathProvider.GetJobsConfigPath();

        if (!File.Exists(path))
            return [];

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<BackupJob>>(json, _jsonOptions)
                   ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    /// Saves jobs to the JSON file.
    /// </summary>
    /// <param name="all">List of jobs to save.</param>
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
