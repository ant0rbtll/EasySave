using System.Text.Json;
using EasySave.Configuration;
using EasySave.Core;

namespace EasySave.Persistence;

public class JsonSaveWorkRepository : ISaveWorkRepository
{
    private readonly int _maxJobs = 5;
    private readonly IPathProvider _pathProvider;
    private readonly IJobIdProvider _idProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonSaveWorkRepository(IPathProvider pathProvider, IJobIdProvider idProvider)
    {
        _pathProvider = pathProvider;
        _idProvider = idProvider;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public void Add(SaveWork job)
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

    public void Remove(int id)
    {
        var all = Load();
        var job = all.FirstOrDefault(j => j.Id == id);

        if (job == null)
            throw new KeyNotFoundException($"Job with ID {id} not found.");

        all.Remove(job);
        Save(all);
    }

    public SaveWork GetById(int id)
    {
        var all = Load();
        var job = all.FirstOrDefault(j => j.Id == id);

        if (job == null)
            throw new KeyNotFoundException($"Job with ID {id} not found.");

        return job;
    }

    public List<SaveWork> GetAll()
    {
        return Load();
    }

    public int Count()
    {
        return Load().Count;
    }

    public int MaxJobs()
    {
        return _maxJobs;
    }

    private List<SaveWork> Load()
    {
        var path = _pathProvider.GetJobsConfigPath();

        if (!File.Exists(path))
            return new List<SaveWork>();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<SaveWork>>(json, _jsonOptions) 
                   ?? new List<SaveWork>();
        }
        catch (JsonException)
        {
            // Fichier corrompu, retourner liste vide
            return new List<SaveWork>();
        }
    }
    
    private void Save(List<SaveWork> all)
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
