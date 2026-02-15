namespace EasySave.Log;

/// <summary>
/// Provides runtime logger reconfiguration capabilities.
/// </summary>
public interface ILoggerRuntimeReloader
{
    /// <summary>
    /// Reloads logger configuration from persisted preferences.
    /// </summary>
    void Reload();
}
