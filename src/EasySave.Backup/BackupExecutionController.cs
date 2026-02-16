using System.Threading;

namespace EasySave.Backup;

/// <summary>
/// Thread-safe execution controller for pause/resume/stop.
/// </summary>
public sealed class BackupExecutionController : IBackupExecutionController, IDisposable
{
    private const string PauseActionKey = "action_backup_paused_by_user";
    private const string StopActionKey = "action_backup_stopped_by_user";

    private readonly object _gate = new();
    private readonly ManualResetEventSlim _resumeEvent = new(initialState: true);
    private readonly Queue<string> _pendingActions = new();

    private int? _currentJobId;
    private bool _isPaused;
    private bool _stopRequested;
    private bool _disposed;

    public void BeginJob(int jobId)
    {
        lock (_gate)
        {
            _currentJobId = jobId;
            _isPaused = false;
            _stopRequested = false;
            _pendingActions.Clear();
            _resumeEvent.Set();
        }
    }

    public void EndJob(int jobId)
    {
        lock (_gate)
        {
            if (_currentJobId != jobId)
                return;

            _currentJobId = null;
            _isPaused = false;
            _stopRequested = false;
            _pendingActions.Clear();
            _resumeEvent.Set();
        }
    }

    public void Pause()
    {
        lock (_gate)
        {
            if (_currentJobId is null || _stopRequested)
                return;

            if (_isPaused)
                return;

            _isPaused = true;
            _pendingActions.Enqueue(PauseActionKey);
            _resumeEvent.Reset();
        }
    }

    public void Resume()
    {
        lock (_gate)
        {
            if (_currentJobId is null)
                return;

            _isPaused = false;
            _resumeEvent.Set();
        }
    }

    public void RequestStop()
    {
        lock (_gate)
        {
            if (_currentJobId is null)
                return;

            _stopRequested = true;
            _isPaused = false;
            _resumeEvent.Set();
        }
    }

    public void WaitIfPausedOrThrowIfStopped()
    {
        while (true)
        {
            bool isPaused;
            bool stopRequested;

            lock (_gate)
            {
                stopRequested = _stopRequested;
                isPaused = _isPaused;
            }

            if (stopRequested)
                throw CreateStoppedByUserException();

            if (!isPaused)
                return;

            _resumeEvent.Wait(100);
        }
    }

    public bool TryDequeueAction(out string actionKey)
    {
        lock (_gate)
        {
            if (_pendingActions.Count == 0)
            {
                actionKey = string.Empty;
                return false;
            }

            actionKey = _pendingActions.Dequeue();
            return true;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _resumeEvent.Dispose();
        _disposed = true;
    }

    private static Exception CreateStoppedByUserException()
    {
        var exception = new InvalidOperationException("error_backup_stopped_by_user");
        exception.Data["errorKey"] = "error_backup_stopped_by_user";
        exception.Data["actionKey"] = StopActionKey;
        return exception;
    }
}
