namespace EasySave.Log;

/// <summary>
/// Notifies subscribers when the centralized log server becomes unreachable or recovers.
/// </summary>
public interface ILogServerStatusNotifier
{
    /// <summary>
    /// Raised when the server reachability status changes.
    /// <c>true</c> = server is reachable; <c>false</c> = server is unreachable.
    /// </summary>
    event Action<bool>? ServerStatusChanged;

    /// <summary>
    /// Gets the current known server status.
    /// <c>true</c> = reachable (or not applicable); <c>false</c> = unreachable.
    /// </summary>
    bool IsServerReachable { get; }
}
