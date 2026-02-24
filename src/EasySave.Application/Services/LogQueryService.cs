using EasySave.Application.Readers;
using EasySave.Configuration;
using EasySave.Core;
using EasySave.Log;

namespace EasySave.Application.Services;

/// <summary>
/// Reads log files by date using pluggable format readers.
/// </summary>
public sealed class LogQueryService : LogServiceBase, ILogQueryService
{

    /// <summary>
    /// Initializes a log query service with path resolution and format-specific readers.
    /// </summary>
    /// <param name="pathProvider">Provider used to resolve the logs directory.</param>
    /// <param name="readers">Registered readers keyed by log format.</param>
    public LogQueryService(IPathProvider pathProvider, IEnumerable<ILogReader> readers)
        : base(pathProvider, readers)
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<DateOnly> GetAvailableDates()
    {
        string logsDirectory = _pathProvider.ResolveLogsDirectory();
        if (!Directory.Exists(logsDirectory))
        {
            return [];
        }

        return Enum.GetValues<LogFormat>()
            .SelectMany(format =>
            {
                string extension = LogFileNaming.GetFileExtension(format);
                string pattern = $"*.{extension}";

                return Directory.EnumerateFiles(logsDirectory, pattern, SearchOption.TopDirectoryOnly);
            })
            .Where(static path => LogFileNaming.TryParseDateFromFilePath(path, out _))
            .Select(static path =>
            {
                LogFileNaming.TryParseDateFromFilePath(path, out DateOnly date);
                return date;
            })
            .Distinct()
            .OrderByDescending(static d => d)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> GetByDate(DateOnly date)
    {
        string logsDirectory = _pathProvider.ResolveLogsDirectory();
        if (!Directory.Exists(logsDirectory))
        {
            return [];
        }

        return _readerByFormat
            .SelectMany(pair =>
            {
                string filePath = Path.Combine(logsDirectory, LogFileNaming.BuildFileName(date, pair.Key));
                if (!File.Exists(filePath))
                {
                    return [];
                }

                return pair.Value.ReadEntries(filePath);
            })
            .OrderByDescending(static e => e.Timestamp)
            .ToList();
    }
}
