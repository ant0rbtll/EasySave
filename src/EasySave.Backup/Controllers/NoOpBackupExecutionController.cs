namespace EasySave.Backup;

/// <summary>
/// No-op execution controller used when runtime control is not configured.
/// </summary>
public sealed class NoOpBackupExecutionController : IBackupExecutionController
{
    public void BeginJob(int jobId) { }

    public void EndJob(int jobId) { }

    public void PauseAll() { }

    public void Pause(int jobId) { }

    public void ResumeAll() { }

    public void Resume(int jobId) { }

    public void RequestStopAll() { }

    public void RequestStop(int jobId) { }

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
