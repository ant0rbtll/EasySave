namespace EasySave.Log;

/// <summary>
/// Provides logging capabilities for backup operations.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    void Write(LogEntry entry);
}
