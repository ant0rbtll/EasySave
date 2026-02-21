using EasySave.Core.Exceptions;
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
        EasysaveDefaultException.ThrowIfNull(run);

        if (!_runningJobs.TryAdd(jobId, 0))
        {
            throw new EasysaveDefaultException("error_job_already_running", [jobId.ToString()]);
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
