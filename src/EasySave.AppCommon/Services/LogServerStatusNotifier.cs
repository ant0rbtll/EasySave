using EasySave.Log;

namespace EasySave.AppCommon.Services;

/// <summary>
/// Thread-safe implementation of <see cref="ILogServerStatusNotifier"/>.
/// Only fires the event when the status actually transitions.
/// </summary>
public sealed class LogServerStatusNotifier : ILogServerStatusNotifier
{
    private readonly object _sync = new();
    private bool _isServerReachable = true;

    public event Action<bool>? ServerStatusChanged;

    public bool IsServerReachable
    {
        get { lock (_sync) return _isServerReachable; }
    }

    public void ReportStatus(bool isReachable)
    {
        lock (_sync)
        {
            if (_isServerReachable == isReachable)
                return;

            _isServerReachable = isReachable;
        }

        ServerStatusChanged?.Invoke(isReachable);
    }
}
