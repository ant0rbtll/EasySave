namespace EasyLog;

/// <summary>
/// Defines how log entries are arranged inside a log file.
/// </summary>
public interface ILogFileLayout
{
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
