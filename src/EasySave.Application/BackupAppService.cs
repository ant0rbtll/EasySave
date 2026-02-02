using System.Runtime.InteropServices.Marshalling;
using EasySave.Persistence;
using EasySave.CLI;
using EasySave.Backup;
using EasySave.UI;
using EasySave.Core;

namespace EasySave.Application;

public class BackupAppService
{
    private readonly IBackupJobRepository _repo;
    private readonly BackupEngine _engine;
    private readonly IUI _ui;
    private readonly CommandLineParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupAppService"/> class.
    /// </summary>
    /// <param name="repo">The repository used for data persistence.</param>
    /// <param name="ui">The user interface for displaying messages.</param>
    public BackupAppService(IBackupJobRepository repo, IUI ui)
    {
        _repo = repo;
        _engine = new BackupEngine();
        _ui = ui;
        _parser = new CommandLineParser();
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
    /// Executes a specific list of backup jobs.
    /// </summary>
    /// <param name="ids">Array of job identifiers to launch.</param>
    public void RunJobs(int[] ids)
    {
        foreach (int id in ids)
        {
            var job = _repo.GetById(id);
            if (job != null)
            {
                _engine.Execute(job);
            }
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
            _engine.Execute(job);
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