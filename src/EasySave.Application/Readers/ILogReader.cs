using EasySave.Core;
using EasySave.Log;

namespace EasySave.Application.Readers;

/// <summary>
/// Reads log entries from one concrete file format.
/// </summary>
public interface ILogReader
{
    /// <summary>
    /// Gets the format handled by this reader.
    /// </summary>
    LogFormat Format { get; }

    /// <summary>
    /// Reads entries from a file path.
    /// </summary>
    /// <param name="filePath">Absolute path to the log file.</param>
    /// <returns>Typed log entries.</returns>
    IReadOnlyList<LogEntry> ReadEntries(string filePath);
}
