using System.Threading;

namespace EasySave.Backup;

/// <summary>
/// Thread-safe execution controller for pause/resume/stop.
/// </summary>
public sealed class BackupExecutionController : IBackupExecutionController, IDisposable
{
    private const string PauseActionKey = BackupRuntimeKeys.ActionBackupPausedByUser;
    private const string StopActionKey = BackupRuntimeKeys.ActionBackupStoppedByUser;

    private readonly object _gate = new();
    private readonly Dictionary<int, JobControlState> _jobs = new();
    private readonly Dictionary<int, PendingJobControlState> _pendingJobs = new();
    private readonly AsyncLocal<int?> _currentExecutionJobId = new();

    private bool _disposed;

    public void BeginJob(int jobId)
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            if (!_jobs.TryGetValue(jobId, out var state))
            {
                state = new JobControlState();
                _jobs[jobId] = state;
            }

            state.IsPaused = false;
            state.StopRequested = false;
            state.PendingActions.Clear();
            state.ResumeEvent.Set();

            if (_pendingJobs.Remove(jobId, out var pendingState))
            {
                if (pendingState.StopRequested)
                {
                    state.StopRequested = true;
                    state.IsPaused = false;
                    state.ResumeEvent.Set();
                }
                else if (pendingState.PauseRequested)
                {
                    state.IsPaused = true;
                    state.PendingActions.Enqueue(PauseActionKey);
                    state.ResumeEvent.Reset();
                }
            }
        }

        _currentExecutionJobId.Value = jobId;
    }

    public void EndJob(int jobId)
    {
        JobControlState? stateToDispose = null;

        lock (_gate)
        {
            if (_disposed)
                return;

            if (_jobs.Remove(jobId, out var removed))
            {
                stateToDispose = removed;
            }
        }

        if (_currentExecutionJobId.Value == jobId)
        {
            _currentExecutionJobId.Value = null;
        }

        stateToDispose?.ResumeEvent.Dispose();
    }

    public void Pause()
    {
        PauseForJob(-1);
    }

    public void PauseForJob(int jobId)
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            if (jobId >= 0)
            {
                if (_jobs.TryGetValue(jobId, out var state))
                {
                    ApplyPause(state);
                }
                else
                {
                    SetPendingPause(jobId);
                }

                return;
            }

            foreach (var state in _jobs.Values)
            {
                ApplyPause(state);
            }
        }
    }

    public void Resume()
    {
        ResumeForJob(-1);
    }

    public void ResumeForJob(int jobId)
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            if (jobId >= 0)
            {
                if (_jobs.TryGetValue(jobId, out var state))
                {
                    ApplyResume(state);
                }
                else
                {
                    _pendingJobs.Remove(jobId);
                }

                return;
            }

            foreach (var state in _jobs.Values)
            {
                ApplyResume(state);
            }
        }
    }

    public void RequestStop()
    {
        RequestStopForJob(-1);
    }

    public void RequestStopForJob(int jobId)
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            if (jobId >= 0)
            {
                if (_jobs.TryGetValue(jobId, out var state))
                {
                    ApplyStop(state);
                }
                else
                {
                    SetPendingStop(jobId);
                }

                return;
            }

            foreach (var state in _jobs.Values)
            {
                ApplyStop(state);
            }
        }
    }

    public void WaitIfPausedOrThrowIfStopped()
    {
        var jobId = _currentExecutionJobId.Value;
        if (!jobId.HasValue)
            return;

        while (true)
        {
            bool isPaused = false;
            bool stopRequested = false;
            bool hasJob = false;
            bool isDisposed;
            ManualResetEventSlim? resumeEvent = null;

            lock (_gate)
            {
                isDisposed = _disposed;
                if (!isDisposed && _jobs.TryGetValue(jobId.Value, out var state))
                {
                    hasJob = true;
                    stopRequested = state.StopRequested;
                    isPaused = state.IsPaused;
                    resumeEvent = state.ResumeEvent;
                }
            }

            if (isDisposed)
                return;

            if (!hasJob)
                return;

            if (stopRequested)
                throw CreateStoppedByUserException();

            if (!isPaused)
                return;

            try
            {
                resumeEvent?.Wait(100);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }
    }

    public bool TryDequeueAction(out string actionKey)
    {
        var jobId = _currentExecutionJobId.Value;
        lock (_gate)
        {
            if (_disposed || !jobId.HasValue || !_jobs.TryGetValue(jobId.Value, out var state))
            {
                actionKey = string.Empty;
                return false;
            }

            if (state.PendingActions.Count == 0)
            {
                actionKey = string.Empty;
                return false;
            }

            actionKey = state.PendingActions.Dequeue();
            return true;
        }
    }

    public bool TryGetCurrentJobControlState(int jobId, out BackupJobControlState controlState)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                controlState = default;
                return false;
            }

            if (!_jobs.TryGetValue(jobId, out var state))
            {
                if (_pendingJobs.TryGetValue(jobId, out var pending))
                {
                    controlState = pending.StopRequested
                        ? BackupJobControlState.StopRequested
                        : pending.PauseRequested
                        ? BackupJobControlState.Paused
                        : BackupJobControlState.Running;
                    return true;
                }

                controlState = default;
                return false;
            }

            if (state.StopRequested)
            {
                controlState = BackupJobControlState.StopRequested;
                return true;
            }

            controlState = state.IsPaused
                ? BackupJobControlState.Paused
                : BackupJobControlState.Running;
            return true;
        }
    }

    public void Dispose()
    {
        List<JobControlState> statesToDispose;

        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            statesToDispose = [.. _jobs.Values];
            _jobs.Clear();
            _pendingJobs.Clear();
        }

        foreach (var state in statesToDispose)
        {
            state.ResumeEvent.Dispose();
        }
    }

    private static Exception CreateStoppedByUserException()
    {
        var exception = new InvalidOperationException(BackupRuntimeKeys.ErrorBackupStoppedByUser);
        exception.Data["errorKey"] = BackupRuntimeKeys.ErrorBackupStoppedByUser;
        exception.Data["actionKey"] = StopActionKey;
        return exception;
    }

    private static void ApplyPause(JobControlState state)
    {
        if (state.StopRequested || state.IsPaused)
            return;

        state.IsPaused = true;
        state.PendingActions.Enqueue(PauseActionKey);
        state.ResumeEvent.Reset();
    }

    private static void ApplyResume(JobControlState state)
    {
        state.IsPaused = false;
        state.ResumeEvent.Set();
    }

    private static void ApplyStop(JobControlState state)
    {
        state.StopRequested = true;
        state.IsPaused = false;
        state.ResumeEvent.Set();
    }

    private void SetPendingPause(int jobId)
    {
        if (!_pendingJobs.TryGetValue(jobId, out var pending))
        {
            pending = new PendingJobControlState();
            _pendingJobs[jobId] = pending;
        }

        if (pending.StopRequested)
            return;

        pending.PauseRequested = true;
    }

    private void SetPendingStop(int jobId)
    {
        if (!_pendingJobs.TryGetValue(jobId, out var pending))
        {
            pending = new PendingJobControlState();
            _pendingJobs[jobId] = pending;
        }

        pending.StopRequested = true;
        pending.PauseRequested = false;
    }

    private sealed class PendingJobControlState
    {
        public bool PauseRequested { get; set; }
        public bool StopRequested { get; set; }
    }

    private sealed class JobControlState
    {
        public ManualResetEventSlim ResumeEvent { get; } = new(initialState: true);
        public Queue<string> PendingActions { get; } = new();
        public bool IsPaused { get; set; }
        public bool StopRequested { get; set; }
    }
}
