namespace EasySave.Backup;

/// <summary>
/// No-op execution controller used when runtime control is not configured.
/// </summary>
public sealed class NoOpBackupExecutionController : IBackupExecutionController
{
    public void BeginJob(int jobId) { }

    public void EndJob(int jobId) { }

    public void Pause() { }

    public void PauseForJob(int jobId) { }

    public void Resume() { }

    public void ResumeForJob(int jobId) { }

    public void RequestStop() { }

    public void RequestStopForJob(int jobId) { }

    public void WaitIfPausedOrThrowIfStopped() { }

    public bool TryDequeueAction(out string actionKey)
    {
        actionKey = string.Empty;
        return false;
    }

    public bool TryGetCurrentJobControlState(int jobId, out BackupJobControlState controlState)
    {
        controlState = default;
        return false;
    }
}
