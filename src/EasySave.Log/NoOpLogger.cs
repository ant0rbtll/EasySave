namespace EasySave.Log;

public sealed class NoOpLogger : ILogger
{
    public void Write(LogEntry entry)
    {
        // Intentionally no-op when logging is disabled or unavailable.
    }
}
