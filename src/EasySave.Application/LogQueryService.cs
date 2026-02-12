using EasySave.Configuration;
using EasySave.Core;
using EasySave.Log;

namespace EasySave.Application;

/// <summary>
/// Reads log files by date using pluggable format readers.
/// </summary>
public sealed class LogQueryService : ILogQueryService
{
    private readonly IPathProvider _pathProvider;
    private readonly IReadOnlyDictionary<LogFormat, ILogReader> _readerByFormat;

    /// <summary>
    /// Initializes a log query service with path resolution and format-specific readers.
    /// </summary>
    /// <param name="pathProvider">Provider used to resolve the logs directory.</param>
    /// <param name="readers">Registered readers keyed by log format.</param>
    public LogQueryService(IPathProvider pathProvider, IEnumerable<ILogReader> readers)
    {
        ArgumentNullException.ThrowIfNull(pathProvider);
        ArgumentNullException.ThrowIfNull(readers);

        _pathProvider = pathProvider;

        var grouped = readers.GroupBy(static r => r.Format).ToList();
        var duplicate = grouped.FirstOrDefault(static g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Multiple log readers are registered for format '{duplicate.Key}'.");
        }

        _readerByFormat = grouped.ToDictionary(static g => g.Key, static g => g.Single());
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
    public IReadOnlyList<LogEntry> GetByDate(DateOnly date, LogFormat format)
    {
        var reader = ResolveReader(format);

        string logsDirectory = _pathProvider.ResolveLogsDirectory();
        string filePath = Path.Combine(logsDirectory, LogFileNaming.BuildFileName(date, format));

        if (!File.Exists(filePath))
        {
            return [];
        }

        return reader.ReadEntries(filePath);
    }

    /// <summary>
    /// Resolves the reader registered for one log format.
    /// </summary>
    /// <param name="format">Requested log format.</param>
    /// <returns>The matching reader implementation.</returns>
    /// <exception cref="NotSupportedException">Thrown when no reader is registered for the format.</exception>
    private ILogReader ResolveReader(LogFormat format)
    {
        if (_readerByFormat.TryGetValue(format, out var reader))
        {
            return reader;
        }

        throw new NotSupportedException($"No log reader is registered for format '{format}'.");
    }
}
