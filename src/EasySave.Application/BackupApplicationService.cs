using EasySave.Persistence;
using EasySave.Backup;
using EasySave.Core;
using EasySave.Log;
using EasySave.Configuration;
using EasySave.State;
using System.Text.Json;

namespace EasySave.Application;

/// <summary>
/// Initializes a new instance of the <see cref="BackupApplicationService"/> class.
/// </summary>
/// <param name="repo">The repository used for data persistence.</param>
/// <param name="backupEngine">The engine responsible for executing backup jobs.</param>
public class BackupApplicationService(
    IBackupJobRepository repo,
    IBackupEngine backupEngine,
    IPathProvider? pathProvider = null)
{
    private readonly IBackupJobRepository _repo = repo;
    private readonly IBackupEngine _engine = backupEngine;
    private readonly IPathProvider? _pathProvider = pathProvider;

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
    /// Executes a specific backup job by its identifier.
    /// </summary>
    /// <param name="id">Identifier of the job to run.</param>
    public void RunJob(int id)
    {
        var job = _repo.GetById(id);
        if (job != null) ExecuteJob(job);
    }

    /// <summary>
    /// Executes a specific list of backup jobs.
    /// </summary>
    /// <param name="ids">Array of job identifiers to launch.</param>
    public void RunJobs(int[] ids)
    {
        foreach (int id in ids)
        {
            RunJob(id);
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
            ExecuteJob(job);
        }
    }

    /// <summary>
    /// Retrieves all backup jobs from the repository.
    /// </summary>
    /// <returns>A list of <see cref="BackupJob"/> objects.</returns>
    public List<BackupJob> GetAllJobs()
    {
        var jobs = _repo.GetAll();
        var stateEntries = LoadStateEntries();
        foreach (var job in jobs)
        {
            ApplyState(job, stateEntries);
        }
        return jobs;
    }

    /// <summary>
    /// Retrieves a specific backup job by ID.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>The BackupJob if found, null otherwise.</returns>
    public BackupJob? GetJob(int id)
    {
        var job = _repo.GetById(id);
        if (job == null) return null;
        ApplyState(job, LoadStateEntries());
        return job;
    }

    /// <summary>
    /// Updates an existing backup job with new values.
    /// </summary>
    /// <param name="job">The backup job with updated values.</param>
    public void UpdateJob(BackupJob job)
    {
        _repo.Update(job);
    }
    private void ExecuteJob(BackupJob job)
    {
        _engine.Execute(job);
    }

    private Dictionary<int, StateEntry> LoadStateEntries()
    {
        if (_pathProvider is null)
        {
            return new Dictionary<int, StateEntry>();
        }

        try
        {
            string path = _pathProvider.GetStatePath();
            if (!File.Exists(path))
            {
                return new Dictionary<int, StateEntry>();
            }

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<int, StateEntry>();
            }

            return JsonSerializer.Deserialize<Dictionary<int, StateEntry>>(json)
                   ?? new Dictionary<int, StateEntry>();
        }
        catch (JsonException)
        {
            return new Dictionary<int, StateEntry>();
        }
        catch (IOException)
        {
            return new Dictionary<int, StateEntry>();
        }
        catch (UnauthorizedAccessException)
        {
            return new Dictionary<int, StateEntry>();
        }
    }

    private static void ApplyState(BackupJob job, Dictionary<int, StateEntry> entries)
    {
        if (entries.TryGetValue(job.Id, out var entry))
        {
            job.LastExecutionDate = entry.Timestamp;
            // IsActive represents the current runtime state from the state file.
            job.IsActive = entry.Status == BackupStatus.Active;
        }
        else
        {
            job.LastExecutionDate = null;
            job.IsActive = false;
        }
    }


    /// <summary>
    /// Get all the dates there has been logs
    /// </summary>
    /// <returns>a list of dates</returns>
    public List<string> GetLogsDate()
    {
        List<string> dates = new();
        var logsPath = _pathProvider.ResolveLogsDirectory();

        string[] files = Directory.GetFiles(logsPath);
        foreach (string file in files)
        {
            dates.Add(Path.GetFileNameWithoutExtension(file));
        }
        return dates;
    } 

    ///// <summary>
    ///// YYYY-MM-DD
    ///// Get the logs by a date
    ///// </summary>
    ///// <param name="date">The date of the searching logs</param>
    ///// <returns> The logs of the date given</returns>
    //public List<LogEntry> GetLogsByDate(string date)
    //{

    //}
}