using EasySave.Persistence;
using EasySave.Backup;
//using EasySave.CLI;
using EasySave.Core;
using EasySave.Localization;

namespace EasySave.Application;

public class BackupAppService
{
    private readonly IBackupJobRepository _repo;
    private readonly BackupEngine _engine;
    private readonly IUI _ui;
    // private readonly CommandLineParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupAppService"/> class.
    /// </summary>
    /// <param name="repo">The repository used for data persistence.</param>
    /// <param name="ui">The user interface for displaying messages.</param>
    /// <param name="backupEngine">The engine responsible for executing backup jobs.</param>
    public BackupAppService(IBackupJobRepository repo, IUI ui, BackupEngine backupEngine)
    {
        _repo = repo;
        _engine = backupEngine;
        _ui = ui;
        // _parser = new CommandLineParser();
    }

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
    public void RunJob(BackupJob job)
    {
        try
        {
            _ui.ShowMessage(LocalizationKey.backup_saving);
            _engine.Execute(job);
        }
        catch
        {
            _ui.ShowMessage(LocalizationKey.backup_error);
        }
    }

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
    /// Displays the list of backup jobs in the user interface 
    /// and returns the complete list from the repository.
    /// </summary>
    /// <returns>A list of <see cref="BackupJob"/> objects.</returns>
    public List<BackupJob> GetAllJobs()
    {
        var jobs = _repo.GetAll();
        foreach (var job in jobs)
        {
            _ui.ShowMessage($"[{job.Id}] {job.Name} | {job.Source} -> {job.Destination} ({job.Type})");
        }
        return jobs;
    }
}