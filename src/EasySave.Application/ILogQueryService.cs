using EasySave.Core;
using EasySave.Log;

namespace EasySave.Application;

/// <summary>
/// Provides read access to persisted daily logs.
/// </summary>
public interface ILogQueryService
{
    /// <summary>
    /// Lists available log dates for a specific format.
    /// </summary>
    /// <param name="format">The log file format to search for.</param>
    /// <returns>Dates sorted from newest to oldest.</returns>
    IReadOnlyList<DateOnly> GetAvailableDates(LogFormat format);

    /// <summary>
    /// Reads all entries for a date and format.
    /// </summary>
    /// <param name="date">The target day.</param>
    /// <param name="format">The log file format.</param>
    /// <returns>Typed log entries. Returns an empty list when the file does not exist.</returns>
    IReadOnlyList<LogEntry> GetByDate(DateOnly date, LogFormat format);
}
