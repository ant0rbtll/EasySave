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

            foreach (var state in GetTargetStates(jobId))
            {
                if (state.StopRequested || state.IsPaused)
                    continue;

                state.IsPaused = true;
                state.PendingActions.Enqueue(PauseActionKey);
                state.ResumeEvent.Reset();
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

            foreach (var state in GetTargetStates(jobId))
            {
                state.IsPaused = false;
                state.ResumeEvent.Set();
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

            foreach (var state in GetTargetStates(jobId))
            {
                state.StopRequested = true;
                state.IsPaused = false;
                state.ResumeEvent.Set();
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

    private IEnumerable<JobControlState> GetTargetStates(int jobId)
    {
        if (jobId < 0)
        {
            return _jobs.Values;
        }

        if (_jobs.TryGetValue(jobId, out var state))
        {
            return [state];
        }

        return [];
    }

    private sealed class JobControlState
    {
        public ManualResetEventSlim ResumeEvent { get; } = new(initialState: true);
        public Queue<string> PendingActions { get; } = new();
        public bool IsPaused { get; set; }
        public bool StopRequested { get; set; }
    }
}
