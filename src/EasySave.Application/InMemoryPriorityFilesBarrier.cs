using EasySave.System;

namespace EasySave.Application;

/// <summary>
/// In-memory global barrier for priority files across running jobs.
/// </summary>
public sealed class InMemoryPriorityFilesBarrier : IPriorityFilesBarrier
{
    private readonly object _sync = new();
    private readonly Dictionary<int, int> _pendingPriorityByJob = new();
    private int _pendingPriorityTotal;
    private TaskCompletionSource<bool> _noPendingPrioritySignal = CompletedSignal();

    public void RegisterJob(int jobId, int pendingPriorityFiles)
    {
        if (pendingPriorityFiles < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pendingPriorityFiles));
        }

        lock (_sync)
        {
            if (_pendingPriorityByJob.ContainsKey(jobId))
            {
                return;
            }

            if (_pendingPriorityTotal == 0 && pendingPriorityFiles > 0)
            {
                _noPendingPrioritySignal = PendingSignal();
            }

            _pendingPriorityByJob[jobId] = pendingPriorityFiles;
            _pendingPriorityTotal += pendingPriorityFiles;

            if (_pendingPriorityTotal == 0)
            {
                _noPendingPrioritySignal.TrySetResult(true);
            }
        }
    }

    public void MarkPriorityFileCompleted(int jobId)
    {
        lock (_sync)
        {
            if (!_pendingPriorityByJob.TryGetValue(jobId, out var pending) || pending <= 0)
            {
                return;
            }

            pending--;
            _pendingPriorityByJob[jobId] = pending;
            _pendingPriorityTotal--;

            if (_pendingPriorityTotal == 0)
            {
                _noPendingPrioritySignal.TrySetResult(true);
            }
        }
    }

    public Task WaitUntilNoPriorityPendingAsync(CancellationToken cancellationToken = default)
    {
        Task waitTask;
        lock (_sync)
        {
            waitTask = _noPendingPrioritySignal.Task;
        }

        return waitTask.WaitAsync(cancellationToken);
    }

    public void UnregisterJob(int jobId)
    {
        lock (_sync)
        {
            if (!_pendingPriorityByJob.Remove(jobId, out var pendingForJob))
            {
                return;
            }

            if (pendingForJob > 0)
            {
                _pendingPriorityTotal -= pendingForJob;
            }

            if (_pendingPriorityTotal == 0)
            {
                _noPendingPrioritySignal.TrySetResult(true);
            }
        }
    }

    private static TaskCompletionSource<bool> PendingSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<bool> CompletedSignal()
    {
        var tcs = PendingSignal();
        tcs.TrySetResult(true);
        return tcs;
    }
}
