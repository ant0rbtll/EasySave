using System.Collections.Concurrent;

namespace EasySave.Application;

/// <summary>
/// In-memory coordinator that allows parallel runs across different jobs
/// while preventing duplicate execution for the same job id.
/// </summary>
public sealed class InMemoryBackupRunCoordinator : IBackupRunCoordinator
{
    private readonly ConcurrentDictionary<int, byte> _runningJobs = new();

    public bool IsRunning(int jobId) => _runningJobs.ContainsKey(jobId);

    public async Task RunExclusiveAsync(int jobId, Func<CancellationToken, Task> run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (!_runningJobs.TryAdd(jobId, 0))
        {
            var exception = new InvalidOperationException($"Job with ID {jobId} is already running.");
            exception.Data["errorKey"] = "error_job_already_running";
            exception.Data["job_id"] = jobId;
            throw exception;
        }

        try
        {
            await run(cancellationToken);
        }
        finally
        {
            _runningJobs.TryRemove(jobId, out _);
        }
    }
}
