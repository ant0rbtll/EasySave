using EasySave.Exceptions;
using EasySave.System;

namespace EasySave.Application;

/// <summary>
/// In-memory global barrier for priority files across running jobs.
/// </summary>
public sealed class InMemoryPriorityFilesBarrier : IPriorityFilesBarrier
{
    private readonly object _sync = new();
    private readonly Dictionary<int, JobPriorityState> _jobStates = new();
    private int _effectivePendingPriorityTotal;
    private TaskCompletionSource<bool> _noPendingPrioritySignal = CompletedSignal();

    public void RegisterJob(int jobId, int pendingPriorityFiles)
    {
        if (pendingPriorityFiles < 0)
        {
            throw new EasysaveDefaultException(Localization.LocalizationKey.error_out_of_range, [nameof(pendingPriorityFiles)]);
        }

        lock (_sync)
        {
            if (_jobStates.ContainsKey(jobId))
            {
                return;
            }

            if (_effectivePendingPriorityTotal == 0 && pendingPriorityFiles > 0)
            {
                _noPendingPrioritySignal = PendingSignal();
            }

            _jobStates[jobId] = new JobPriorityState(pendingPriorityFiles);
            _effectivePendingPriorityTotal += pendingPriorityFiles;

            if (_effectivePendingPriorityTotal == 0)
            {
                _noPendingPrioritySignal.TrySetResult(true);
            }
        }
    }

    public void MarkPriorityFileCompleted(int jobId)
    {
        lock (_sync)
        {
            if (!_jobStates.TryGetValue(jobId, out var state) || state.Remaining <= 0)
            {
                return;
            }

            state.Remaining--;
            if (!state.IsPaused)
            {
                _effectivePendingPriorityTotal--;
            }

            if (_effectivePendingPriorityTotal == 0)
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

    public void PauseJob(int jobId)
    {
        lock (_sync)
        {
            if (!_jobStates.TryGetValue(jobId, out var state) || state.IsPaused)
            {
                return;
            }

            state.IsPaused = true;
            if (state.Remaining > 0)
            {
                _effectivePendingPriorityTotal -= state.Remaining;
                if (_effectivePendingPriorityTotal == 0)
                {
                    _noPendingPrioritySignal.TrySetResult(true);
                }
            }
        }
    }

    public void ResumeJob(int jobId)
    {
        lock (_sync)
        {
            if (!_jobStates.TryGetValue(jobId, out var state) || !state.IsPaused)
            {
                return;
            }

            state.IsPaused = false;
            if (state.Remaining > 0)
            {
                if (_effectivePendingPriorityTotal == 0)
                {
                    _noPendingPrioritySignal = PendingSignal();
                }

                _effectivePendingPriorityTotal += state.Remaining;
            }

            if (_effectivePendingPriorityTotal == 0)
            {
                _noPendingPrioritySignal.TrySetResult(true);
            }
        }
    }

    public void UnregisterJob(int jobId)
    {
        lock (_sync)
        {
            if (!_jobStates.Remove(jobId, out var state))
            {
                return;
            }

            if (!state.IsPaused && state.Remaining > 0)
            {
                _effectivePendingPriorityTotal -= state.Remaining;
            }

            if (_effectivePendingPriorityTotal == 0)
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

    private sealed class JobPriorityState(int remaining)
    {
        public int Remaining { get; set; } = remaining;
        public bool IsPaused { get; set; }
    }
}
