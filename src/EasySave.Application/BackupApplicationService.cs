using EasySave.Persistence;
using EasySave.Backup;
using EasySave.Core;
using EasySave.State;
using EasySave.System;

namespace EasySave.Application;

/// <summary>
/// Initializes a new instance of the <see cref="BackupApplicationService"/> class.
/// </summary>
/// <param name="repo">The repository used for data persistence.</param>
/// <param name="backupEngine">The engine responsible for executing backup jobs.</param>
public class BackupApplicationService(
    IBackupJobRepository repo,
    IBackupEngine backupEngine,
    IBackupJobStateService backupJobStateService,
    IStateReader? stateReader = null,
    IBackupExecutionGuard? backupExecutionGuard = null)
{
    private readonly IBackupJobRepository _repo = repo;
    private readonly IBackupEngine _engine = backupEngine;
    private readonly IBackupJobStateService _backupJobStateService = backupJobStateService;
    private readonly IStateReader? _stateReader = stateReader;
    private readonly IBackupExecutionGuard _backupExecutionGuard = backupExecutionGuard ?? new NoOpBackupExecutionGuard();

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
        EnsureBusinessSoftwareIsNotRunning();

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
            EnsureBusinessSoftwareIsNotRunning();
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
        _backupJobStateService.ApplyState(jobs);
        return jobs;
    }

    /// <summary>
    /// Retrieves runtime state for all backup jobs keyed by job identifier.
    /// </summary>
    public IReadOnlyDictionary<int, BackupJobRuntimeState> GetAllJobsRuntimeStates()
    {
        var jobs = _repo.GetAll();
        _backupJobStateService.ApplyState(jobs);

        return jobs.ToDictionary(
            j => j.Id,
            j => new BackupJobRuntimeState(j.Id, j.Status, j.LastExecutionDate, j.IsActive));
    }

    /// <summary>
    /// Retrieves real-time progress information for active backup executions only.
    /// </summary>
    public IReadOnlyDictionary<int, BackupJobLiveProgressState> GetAllJobsLiveProgress()
    {
        var entries = _stateReader?.ReadEntries() ?? new Dictionary<int, StateEntry>();
        var jobNamesById = (_repo.GetAll() ?? [])
            .ToDictionary(j => j.Id, j => j.Name);
        var states = new Dictionary<int, BackupJobLiveProgressState>(entries.Count);
        foreach (var (jobId, entry) in entries)
        {
            if (entry.Status != BackupStatus.Active)
            {
                continue;
            }

            var jobName = ResolveNonEmptyJobName(entry.BackupName, jobId, jobNamesById);
            states[jobId] = new BackupJobLiveProgressState(
                jobId,
                jobName,
                BackupJobStatus.Active,
                ClampProgress(entry.ProgressPercent),
                entry.TotalFiles,
                entry.TotalSizeBytes,
                entry.RemainingFiles,
                entry.RemainingSizeBytes,
                entry.CurrentSourcePath,
                entry.CurrentDestinationPath,
                entry.Timestamp);
        }

        return states;
    }

    private static string ResolveNonEmptyJobName(string? stateName, int jobId, IReadOnlyDictionary<int, string> jobNamesById)
    {
        if (!string.IsNullOrWhiteSpace(stateName))
        {
            return stateName;
        }

        if (jobNamesById.TryGetValue(jobId, out var repositoryName)
            && !string.IsNullOrWhiteSpace(repositoryName))
        {
            return repositoryName;
        }

        return $"Job #{jobId}";
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
        _backupJobStateService.ApplyState(job);
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

    /// <summary>
    /// Delegates job execution to the backup engine.
    /// </summary>
    /// <param name="job">The job instance to execute.</param>
    private void ExecuteJob(BackupJob job)
    {
        _engine.Execute(job);
    }

    private void EnsureBusinessSoftwareIsNotRunning()
    {
        _backupExecutionGuard.EnsureCanCopyNextFile();
    }

    private static BackupJobStatus MapStatus(BackupStatus status)
    {
        return status switch
        {
            BackupStatus.Inactive => BackupJobStatus.Inactive,
            BackupStatus.Active => BackupJobStatus.Active,
            BackupStatus.Done => BackupJobStatus.Done,
            BackupStatus.Error => BackupJobStatus.Error,
            _ => BackupJobStatus.Inactive
        };
    }

    private static int ClampProgress(int progressPercent) => Math.Clamp(progressPercent, 0, 100);
}
