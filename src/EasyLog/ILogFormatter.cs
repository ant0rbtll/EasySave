namespace EasyLog;

public interface ILogFormatter
{
    /// <summary>
    /// Formats a single log entry as a JSON object string (not the whole file).
    /// </summary>
    string Format(LogEntry entry);
}