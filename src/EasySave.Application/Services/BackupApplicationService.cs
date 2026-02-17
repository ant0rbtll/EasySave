using EasySave.Persistence;
using EasySave.Backup;
using EasySave.Core;
using EasySave.State;
using EasySave.System;
using System.Threading;
using EasySave.Application.Readers;

namespace EasySave.Application.Services;

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
    IBackupRunCoordinator? backupRunCoordinator = null,
    IBackupExecutionGuard? backupExecutionGuard = null,
    IStateWriter? stateWriter = null,
    IUserPreferencesRepository? userPreferencesRepository = null,
    IBackupEtaEstimator? backupEtaEstimator = null)
{
    private readonly IBackupJobRepository _repo = repo;
    private readonly IBackupEngine _engine = backupEngine;
    private readonly IBackupJobStateService _backupJobStateService = backupJobStateService;
    private readonly IStateReader? _stateReader = stateReader;
    private readonly IBackupRunCoordinator _backupRunCoordinator = backupRunCoordinator ?? new InMemoryBackupRunCoordinator();
    private readonly IBackupExecutionGuard _backupExecutionGuard = backupExecutionGuard ?? new NoOpBackupExecutionGuard();
    private readonly IStateWriter? _stateWriter = stateWriter;
    private readonly IUserPreferencesRepository? _userPreferencesRepository = userPreferencesRepository;
    private readonly IBackupEtaEstimator _backupEtaEstimator = backupEtaEstimator ?? new BackupEtaEstimator();
    private int _activeStateReconciled;

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
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunJob(int id, CancellationToken cancellationToken = default)
    {
        var job = _repo.GetById(id);
        if (job is null)
        {
            var exception = new InvalidOperationException("error_job_not_found");
            exception.Data["errorKey"] = "error_job_not_found";
            exception.Data["0"] = id.ToString();
            throw exception;
        }

        await RunJobInternalAsync(job, cancellationToken);
    }

    /// <summary>
    /// Executes a specific list of backup jobs.
    /// </summary>
    /// <param name="ids">Array of job identifiers to launch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunJobs(int[] ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        await Task.WhenAll(ids.Select(id => RunJob(id, cancellationToken)));
    }

    /// <summary>
    /// Retrieves and executes all registered backup jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAllJobs(CancellationToken cancellationToken = default)
    {
        EnsureBusinessSoftwareIsNotRunning();

        var jobs = _repo.GetAll();
        await Task.WhenAll(jobs.Select(job => RunJobInternalAsync(job, cancellationToken)));
    }

    /// <summary>
    /// Retrieves all backup jobs from the repository.
    /// </summary>
    /// <returns>A list of <see cref="BackupJob"/> objects.</returns>
    public List<BackupJob> GetAllJobs()
    {
        EnsureActiveStateConsistency();
        var jobs = _repo.GetAll();
        _backupJobStateService.ApplyState(jobs);
        return jobs;
    }

    /// <summary>
    /// Retrieves runtime state for all backup jobs keyed by job identifier.
    /// </summary>
    public IReadOnlyDictionary<int, BackupJobRuntimeState> GetAllJobsRuntimeStates()
    {
        EnsureActiveStateConsistency();
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
        EnsureActiveStateConsistency();
        var entries = _stateReader?.ReadEntries() ?? new Dictionary<int, StateEntry>();
        var jobNamesById = (_repo.GetAll() ?? [])
            .ToDictionary(j => j.Id, j => j.Name);
        var states = new Dictionary<int, BackupJobLiveProgressState>(entries.Count);
        var trackedJobIds = new HashSet<int>();
        foreach (var (jobId, entry) in entries)
        {
            var shouldInclude = entry.Status == BackupStatus.Active
                || entry.Status == BackupStatus.Waiting
                || entry.Status == BackupStatus.Paused
                || entry.Status == BackupStatus.Blocked;

            if (!shouldInclude)
            {
                continue;
            }

            var jobName = ResolveNonEmptyJobName(entry.BackupName, jobId, jobNamesById);
            var observedAtUtc = ToUtc(entry.Timestamp);
            var etaSnapshot = _backupEtaEstimator.UpdateEstimate(
                jobId,
                entry.Status,
                entry.TotalFiles,
                entry.RemainingFiles,
                entry.TotalSizeBytes,
                entry.RemainingSizeBytes,
                observedAtUtc);
            trackedJobIds.Add(jobId);
            states[jobId] = new BackupJobLiveProgressState(
                jobId,
                jobName,
                MapStatus(entry.Status),
                ClampProgress(entry.ProgressPercent),
                entry.TotalFiles,
                entry.TotalSizeBytes,
                entry.RemainingFiles,
                entry.RemainingSizeBytes,
                entry.CurrentSourcePath,
                entry.CurrentDestinationPath,
                entry.Timestamp,
                etaSnapshot.EstimatedRemainingTime,
                etaSnapshot.SmoothedThroughputBytesPerSecond);
        }

        _backupEtaEstimator.Prune(trackedJobIds);
        return states;
    }

    private static DateTime ToUtc(DateTime timestamp)
    {
        return timestamp.Kind switch
        {
            DateTimeKind.Utc => timestamp,
            DateTimeKind.Local => timestamp.ToUniversalTime(),
            _ => DateTime.SpecifyKind(timestamp, DateTimeKind.Local).ToUniversalTime()
        };
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
        EnsureActiveStateConsistency();
        var job = _repo.GetById(id);
        if (job == null) return null;
        _backupJobStateService.ApplyState(job);
        return job;
    }

    /// <summary>
    /// Reconciles stale runtime state at application startup.
    /// </summary>
    public void ReconcileStartupState()
    {
        EnsureActiveStateConsistency();
    }

    private void EnsureActiveStateConsistency()
    {
        if (Interlocked.Exchange(ref _activeStateReconciled, 1) == 1)
            return;

        if (_stateReader is null || _stateWriter is null)
            return;

        var entries = _stateReader.ReadEntries();
        foreach (var (jobId, entry) in entries)
        {
            if (entry.Status is not (BackupStatus.Active or BackupStatus.Waiting or BackupStatus.Paused or BackupStatus.Blocked))
                continue;

            if (_backupRunCoordinator.IsRunning(jobId))
                continue;

            _stateWriter.Update(new StateEntry
            {
                BackupId = jobId,
                BackupName = entry.BackupName,
                Timestamp = DateTime.Now,
                Status = BackupStatus.Inactive,
                TotalFiles = entry.TotalFiles,
                TotalSizeBytes = entry.TotalSizeBytes,
                RemainingFiles = entry.RemainingFiles,
                RemainingSizeBytes = entry.RemainingSizeBytes,
                ProgressPercent = entry.ProgressPercent,
                CurrentSourcePath = entry.CurrentSourcePath,
                CurrentDestinationPath = entry.CurrentDestinationPath
            });
        }
    }

    /// <summary>
    /// Updates an existing backup job with new values.
    /// </summary>
    /// <param name="job">The backup job with updated values.</param>
    public void UpdateJob(BackupJob job)
    {
        _repo.Update(job);
    }

    private Task RunJobInternalAsync(BackupJob job, CancellationToken cancellationToken)
    {
        var preferences = _userPreferencesRepository?.Load();
        var executionContext = new BackupExecutionContext(
            preferences?.PriorityExtensions,
            preferences?.GetParallelLargeFileThresholdBytes() ?? 0);

        return _backupRunCoordinator.RunExclusiveAsync(
            job.Id,
            ct => _engine.Execute(job, ct, executionContext),
            cancellationToken);
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
            BackupStatus.Waiting => BackupJobStatus.Waiting,
            BackupStatus.Done => BackupJobStatus.Done,
            BackupStatus.Error => BackupJobStatus.Error,
            BackupStatus.Paused => BackupJobStatus.Paused,
            BackupStatus.Blocked => BackupJobStatus.Blocked,
            _ => BackupJobStatus.Inactive
        };
    }

    private static int ClampProgress(int progressPercent) => Math.Clamp(progressPercent, 0, 100);
}
