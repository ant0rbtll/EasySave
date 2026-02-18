using EasySave.Log;

namespace EasySave.AppCommon.Services;

/// <summary>
/// Thread-safe implementation of <see cref="ILogServerStatusNotifier"/>.
/// Only fires the event when the status actually transitions.
/// </summary>
public sealed class LogServerStatusNotifier : ILogServerStatusNotifier
{
    private volatile bool _isServerReachable = true;

    public event Action<bool>? ServerStatusChanged;

    public bool IsServerReachable => _isServerReachable;

    public void ReportStatus(bool isReachable)
    {
        if (_isServerReachable == isReachable)
            return;

        _isServerReachable = isReachable;
        ServerStatusChanged?.Invoke(isReachable);
    }
}
