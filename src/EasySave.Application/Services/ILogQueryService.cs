using EasySave.Core;
using EasySave.Log;

namespace EasySave.Application.Services;

/// <summary>
/// Provides read access to persisted daily logs.
/// </summary>
public interface ILogQueryService
{
    /// <summary>
    /// Lists available log dates across all supported formats.
    /// </summary>
    /// <returns>Distinct dates sorted from newest to oldest.</returns>
    IReadOnlyList<DateOnly> GetAvailableDates();

    /// <summary>
    /// Reads all entries for a date across all supported formats.
    /// </summary>
    /// <param name="date">The target day.</param>
    /// <returns>Typed log entries. Returns an empty list when no file exists for that date.</returns>
    IReadOnlyList<LogEntry> GetByDate(DateOnly date);
}
