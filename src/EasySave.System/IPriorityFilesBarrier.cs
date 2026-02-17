namespace EasySave.System;

/// <summary>
/// Coordinates global priority-file execution across running backup jobs.
/// </summary>
public interface IPriorityFilesBarrier
{
    /// <summary>
    /// Registers a running job and the number of priority files it still has to process.
    /// </summary>
    void RegisterJob(int jobId, int pendingPriorityFiles);

    /// <summary>
    /// Marks one priority file as processed for a job.
    /// </summary>
    void MarkPriorityFileCompleted(int jobId);

    /// <summary>
    /// Waits until there are no pending priority files across all running jobs.
    /// </summary>
    Task WaitUntilNoPriorityPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a job and releases any remaining pending priority files for that job.
    /// </summary>
    void UnregisterJob(int jobId);
}

/// <summary>
/// Default no-op barrier that never blocks.
/// </summary>
public sealed class NoOpPriorityFilesBarrier : IPriorityFilesBarrier
{
    public void RegisterJob(int jobId, int pendingPriorityFiles)
    {
    }

    public void MarkPriorityFileCompleted(int jobId)
    {
    }

    public Task WaitUntilNoPriorityPendingAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void UnregisterJob(int jobId)
    {
    }
}
