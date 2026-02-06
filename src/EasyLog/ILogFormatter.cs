using EasySave.Log;

namespace EasyLog;

public interface ILogFormatter
{
    /// <summary>
    /// Formats a single log entry as a JSON object string (not the whole file).
    /// </summary>
    string Format(EasySave.Log.LogEntry entry);

    /// <summary>
    /// Returns the file header to write when creating a new log file.
    /// </summary>
    string GetFileHeader() => string.Empty;

    /// <summary>
    /// Returns the file footer to write when closing a log file.
    /// </summary>
    string GetFileFooter() => string.Empty;

    /// <summary>
    /// Returns the separator between log entries.
    /// </summary>
    string GetEntrySeparator() => string.Empty;

    /// <summary>
    /// Returns the number of spaces to indent log entries in the file.
    /// </summary>
    int GetIndentSpaces() => 2;
}

