using EasySave.Persistence;
using EasySave.Backup;
//using EasySave.CLI;
using EasySave.Core;

namespace EasySave.Application;

public class BackupAppService
{
    private readonly IBackupJobRepository _repo;
    private readonly IUI _ui;
    private readonly BackupEngine _engine;
    private readonly IEventService _eventService;

    // private readonly CommandLineParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupAppService"/> class.
    /// </summary>
    /// <param name="repo">The repository used for data persistence.</param>
    /// <param name="ui">The user interface for displaying messages.</param>
    /// <param name="backupEngine">The engine responsible for executing backup jobs.</param>
    public BackupAppService(IBackupJobRepository repo, BackupEngine backupEngine, IEventService eventService, IUI ui)
    {
        _repo = repo;
        _engine = backupEngine;
        _eventService = eventService;
        _ui = ui;
        _eventService.OnCreateJob += HandleCreateJob;
        _eventService.OnGetAllJobsRequested += HandleGetAllJobsRequested;
        _eventService.OnRunJobs += RunJobs;


        // _parser = new CommandLineParser();
    }

    public void RunInteractive()
    {
        _ui.Start();
    }

    /// <summary>
    /// Creates and saves a new backup job.
    /// </summary>
    /// <param name="name">Unique name of the job.</param>
    /// <param name="source">Source folder path.</param>
    /// <param name="destination">Destination folder path.</param>
    /// <param name="type">Type of backup (Full or Differential).</param>
    public void HandleCreateJob(object? sender, CreateJobEventArgs e)
    {
        var job = new BackupJob
        {
            Name = e.Name,
            Source = e.Source,
            Destination = e.Destination,
            Type = e.Type
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
    public void RunJobs(object? sender, LaunchBackupJobsEventArgs e)
    {
        foreach (int id in e.Jobs)
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
    private void HandleGetAllJobsRequested(object sender, GetAllJobsRequestedEventArgs e)
    {
        var jobs = _repo.GetAll();
        _eventService.RaiseAllJobsProvided(jobs);
    }
}