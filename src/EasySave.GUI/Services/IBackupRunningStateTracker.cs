using EasySave.Application;
using EasySave.Backup;

namespace EasySave.GUI.Services;

public interface IBackupRunningStateTracker
{
    IReadOnlySet<int> RunningJobIds { get; }
    bool HasRunningJobs { get; }
    int RunningCount { get; }

    void AddRunning(int jobId);
    void RemoveRunning(int jobId);

    bool SyncFromRuntimeStates(IReadOnlyDictionary<int, BackupJobRuntimeState> runtimeStates);

    bool TryGetCurrentControlJobId(int? pendingJobId, out int jobId);
    bool TryGetRuntimeControlState(int jobId, out BackupJobControlState controlState);

    bool IsJobRunnable(Models.BackupJob job);
    string ResolveRunningJobLabel(int jobId, IReadOnlyList<Models.BackupJob> allJobs);

    bool ComputeIsPaused(int? pendingJobId);
    bool CanPause(int? pendingJobId);
    bool CanPlay(int? pendingJobId);
    bool CanStop(int? pendingJobId);
}
