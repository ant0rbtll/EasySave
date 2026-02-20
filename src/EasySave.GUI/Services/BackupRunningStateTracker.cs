using EasySave.Application;
using EasySave.Backup;
using EasySave.Core;

namespace EasySave.GUI.Services;

public class BackupRunningStateTracker(IBackupExecutionController backupExecutionController) : IBackupRunningStateTracker
{
    private readonly HashSet<int> _runningJobIds = [];
    private readonly IBackupExecutionController _backupExecutionController = backupExecutionController;

    public IReadOnlySet<int> RunningJobIds => _runningJobIds;
    public bool HasRunningJobs => _runningJobIds.Count > 0;
    public int RunningCount => _runningJobIds.Count;

    public void AddRunning(int jobId) => _runningJobIds.Add(jobId);
    public void RemoveRunning(int jobId) => _runningJobIds.Remove(jobId);

    public bool SyncFromRuntimeStates(IReadOnlyDictionary<int, BackupJobRuntimeState> runtimeStates)
    {
        var activeJobIds = runtimeStates
            .Where(pair => pair.Value.IsActive)
            .Select(pair => pair.Key)
            .ToHashSet();

        if (_runningJobIds.SetEquals(activeJobIds))
            return false;

        _runningJobIds.Clear();
        foreach (var id in activeJobIds)
            _runningJobIds.Add(id);

        return true;
    }

    public bool TryGetCurrentControlJobId(int? pendingJobId, out int jobId)
    {
        if (_runningJobIds.Count == 1)
        {
            jobId = _runningJobIds.First();
            return true;
        }

        if (pendingJobId.HasValue && _runningJobIds.Contains(pendingJobId.Value))
        {
            jobId = pendingJobId.Value;
            return true;
        }

        jobId = default;
        return false;
    }

    public bool TryGetRuntimeControlState(int jobId, out BackupJobControlState controlState)
    {
        if (_backupExecutionController.TryGetCurrentJobControlState(jobId, out controlState))
            return true;

        if (_runningJobIds.Contains(jobId))
        {
            controlState = BackupJobControlState.Running;
            return true;
        }

        controlState = default;
        return false;
    }

    public bool IsJobRunnable(Models.BackupJob job)
    {
        return job.Status is not (BackupJobStatus.Active or BackupJobStatus.Waiting
                or BackupJobStatus.Paused or BackupJobStatus.Blocked)
            && !_runningJobIds.Contains(job.Id);
    }

    public string ResolveRunningJobLabel(int jobId, IReadOnlyList<Models.BackupJob> allJobs)
    {
        var job = allJobs.FirstOrDefault(j => j.Id == jobId);
        return job is not null && !string.IsNullOrWhiteSpace(job.Name)
            ? job.Name
            : $"#{jobId}";
    }

    public bool ComputeIsPaused(int? pendingJobId)
    {
        if (_runningJobIds.Count == 0)
            return false;

        if (TryGetCurrentControlJobId(pendingJobId, out var jobId)
            && TryGetRuntimeControlState(jobId, out var controlState))
            return controlState == BackupJobControlState.Paused;

        return _runningJobIds.Count > 0
            && _runningJobIds.All(id =>
                TryGetRuntimeControlState(id, out var state)
                && state == BackupJobControlState.Paused);
    }

    public bool CanPause(int? pendingJobId)
    {
        if (_runningJobIds.Count == 0) return false;

        if (TryGetCurrentControlJobId(pendingJobId, out var jobId))
            return TryGetRuntimeControlState(jobId, out var state)
                && state == BackupJobControlState.Running;

        return _runningJobIds.Any(id =>
            TryGetRuntimeControlState(id, out var state)
            && state == BackupJobControlState.Running);
    }

    public bool CanPlay(int? pendingJobId)
    {
        if (_runningJobIds.Count == 0) return false;

        if (TryGetCurrentControlJobId(pendingJobId, out var jobId))
            return TryGetRuntimeControlState(jobId, out var state)
                && state == BackupJobControlState.Paused;

        return _runningJobIds.Any(id =>
            TryGetRuntimeControlState(id, out var state)
            && state == BackupJobControlState.Paused);
    }

    public bool CanStop(int? pendingJobId)
    {
        if (_runningJobIds.Count == 0) return false;

        if (TryGetCurrentControlJobId(pendingJobId, out var jobId))
            return TryGetRuntimeControlState(jobId, out var state)
                && state != BackupJobControlState.StopRequested;

        return _runningJobIds.Any(id =>
            TryGetRuntimeControlState(id, out var state)
            && state != BackupJobControlState.StopRequested);
    }
}
