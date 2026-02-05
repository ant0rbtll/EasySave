namespace EasySave.Log;

/// <summary>
/// Logger implementation that discards all log entries.
/// Used as a fallback when logging is disabled or unavailable.
/// </summary>
public sealed class NoOpLogger : ILogger
{
    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        // Intentionally no-op when logging is disabled or unavailable.
    }
}
