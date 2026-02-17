using EasySave.Configuration;
using EasySave.Core;
using EasySave.Log;

namespace EasySave.Application;

/// <summary>
/// Provides hierarchical log navigation with file-level caching.
/// </summary>
public sealed class LogNavigationService : ILogNavigationService
{
    private readonly IPathProvider _pathProvider;
    private readonly IReadOnlyDictionary<LogFormat, ILogReader> _readerByFormat;
    private readonly object _sync = new();
    private readonly Dictionary<FileCacheKey, FileCacheValue> _fileCache = new();
    private readonly Dictionary<DateOnly, DayIndexCacheValue> _dayIndexCache = new();

    /// <summary>
    /// Initializes a log navigation service.
    /// </summary>
    /// <param name="pathProvider">Provider used to resolve the logs directory.</param>
    /// <param name="readers">Registered readers keyed by log format.</param>
    public LogNavigationService(IPathProvider pathProvider, IEnumerable<ILogReader> readers)
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
    public IReadOnlyList<LogJobSummary> GetJobsByDate(DateOnly date)
    {
        var index = GetOrBuildDayIndex(date);
        return index.JobSummaries;
    }

    /// <inheritdoc />
    public IReadOnlyList<LogRunSummary> GetRunsByDateAndBackupId(DateOnly date, int backupId)
    {
        var index = GetOrBuildDayIndex(date);
        if (!index.RunsByBackupId.TryGetValue(backupId, out var runs))
        {
            return [];
        }

        return runs
            .OrderByDescending(static run => run.StartTimestamp)
            .ThenByDescending(static run => run.RunId, StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> GetEntriesByRun(DateOnly date, string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            return [];
        }

        var index = GetOrBuildDayIndex(date);
        if (!index.RunsById.TryGetValue(runId, out var run))
        {
            return [];
        }

        if (!index.EntriesByFormat.TryGetValue(run.Format, out var entries))
        {
            return [];
        }

        return entries
            .Where(e =>
                e.Order >= run.StartOrder &&
                e.Order <= run.LastOrder &&
                e.Entry.BackupId == run.BackupId)
            .Select(static e => e.Entry)
            .OrderByDescending(static e => e.Timestamp)
            .ToList();
    }

    private DayIndexCacheValue GetOrBuildDayIndex(DateOnly date)
    {
        string logsDirectory = _pathProvider.ResolveLogsDirectory();
        if (!Directory.Exists(logsDirectory))
        {
            return DayIndexCacheValue.Empty;
        }

        var signatures = new Dictionary<LogFormat, FileSignature>();
        var entriesByFormat = new Dictionary<LogFormat, List<IndexedEntry>>();

        foreach (var pair in _readerByFormat)
        {
            string filePath = Path.Combine(logsDirectory, LogFileNaming.BuildFileName(date, pair.Key));
            if (!TryGetFileSignature(filePath, out var signature))
            {
                continue;
            }

            signatures[pair.Key] = signature;
            entriesByFormat[pair.Key] = GetEntriesForFile(date, pair.Key, filePath, signature, pair.Value);
        }

        lock (_sync)
        {
            if (_dayIndexCache.TryGetValue(date, out var cached) && HasSameSignatureSet(cached.Signatures, signatures))
            {
                return cached;
            }
        }

        var built = BuildDayIndex(signatures, entriesByFormat);

        lock (_sync)
        {
            _dayIndexCache[date] = built;
            return built;
        }
    }

    private List<IndexedEntry> GetEntriesForFile(
        DateOnly date,
        LogFormat format,
        string filePath,
        FileSignature signature,
        ILogReader reader)
    {
        var cacheKey = new FileCacheKey(date, format);

        lock (_sync)
        {
            if (_fileCache.TryGetValue(cacheKey, out var cached) && cached.Signature.Equals(signature))
            {
                return cached.Entries;
            }
        }

        var loaded = reader
            .ReadEntries(filePath)
            .Select((entry, idx) => (Entry: entry, OriginalOrder: idx))
            .OrderBy(static e => e.Entry.Timestamp)
            .ThenBy(static e => e.OriginalOrder)
            .Select((e, sortedOrder) => new IndexedEntry(e.Entry, sortedOrder))
            .ToList();

        lock (_sync)
        {
            _fileCache[cacheKey] = new FileCacheValue(signature, loaded);
            return loaded;
        }
    }

    private static DayIndexCacheValue BuildDayIndex(
        IReadOnlyDictionary<LogFormat, FileSignature> signatures,
        IReadOnlyDictionary<LogFormat, List<IndexedEntry>> entriesByFormat)
    {
        var runs = new List<InternalRun>();

        foreach (var pair in entriesByFormat)
        {
            runs.AddRange(BuildRunsForFormat(pair.Key, pair.Value));
        }

        var runSummaries = runs
            .Select(static run => run.Summary)
            .ToList();

        var runsByBackupId = runSummaries
            .GroupBy(static run => run.BackupId)
            .ToDictionary(static g => g.Key, static g => (IReadOnlyList<LogRunSummary>)g.ToList());

        var jobs = runSummaries
            .GroupBy(static run => run.BackupId)
            .Select(group =>
            {
                var lastRun = group
                    .OrderByDescending(static r => r.StartTimestamp)
                    .First();

                return new LogJobSummary(
                    group.Key,
                    lastRun.BackupName,
                    group.Count());
            })
            .OrderBy(static job => job.BackupName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static job => job.BackupId)
            .ToList();

        var runsById = runs.ToDictionary(static run => run.Summary.RunId, static run => run);

        return new DayIndexCacheValue(
            signatures,
            entriesByFormat.ToDictionary(static p => p.Key, static p => (IReadOnlyList<IndexedEntry>)p.Value),
            runsByBackupId,
            runsById,
            jobs);
    }

    private static List<InternalRun> BuildRunsForFormat(LogFormat format, IReadOnlyList<IndexedEntry> entries)
    {
        if (entries.Count == 0)
        {
            return [];
        }

        var runs = new List<InternalRun>();
        var openRuns = new Dictionary<int, OpenRun>();
        int runOrdinal = 0;
        int lastOrder = entries[^1].Order;

        foreach (var indexed in entries)
        {
            var entry = indexed.Entry;
            int backupId = entry.BackupId;

            if (entry.EventType == LogEventType.StartBackup)
            {
                if (openRuns.TryGetValue(backupId, out var previousOpen))
                {
                    runs.Add(CreateRun(
                        format,
                        previousOpen,
                        indexed.Order - 1,
                        null,
                        LogRunStatus.InProgress,
                        null,
                        null,
                        runOrdinal++));
                }

                openRuns[backupId] = new OpenRun(
                    backupId,
                    entry.BackupName,
                    entry.Timestamp,
                    indexed.Order);
                continue;
            }

            if (entry.EventType == LogEventType.EndBackup)
            {
                if (openRuns.TryGetValue(backupId, out var open))
                {
                    runs.Add(CreateRun(
                        format,
                        open,
                        indexed.Order,
                        entry.Timestamp,
                        LogRunStatus.Completed,
                        entry.TransferTimeMs,
                        entry.FileSizeBytes,
                        runOrdinal++));
                    openRuns.Remove(backupId);
                }
                else
                {
                    var orphanStart = new OpenRun(
                        backupId,
                        entry.BackupName,
                        entry.Timestamp,
                        indexed.Order);

                    runs.Add(CreateRun(
                        format,
                        orphanStart,
                        indexed.Order,
                        entry.Timestamp,
                        LogRunStatus.Completed,
                        entry.TransferTimeMs,
                        entry.FileSizeBytes,
                        runOrdinal++));
                }

                continue;
            }

            if (entry.EventType == LogEventType.Error
                || entry.EventType == LogEventType.BusinessSoftwareDetected
                || entry.EventType == LogEventType.Stopped)
            {
                if (openRuns.TryGetValue(backupId, out var open))
                {
                    runs.Add(CreateRun(
                        format,
                        open,
                        indexed.Order,
                        entry.Timestamp,
                        LogRunStatus.Error,
                        null,
                        null,
                        runOrdinal++));
                    openRuns.Remove(backupId);
                }
                else
                {
                    var orphanStart = new OpenRun(
                        backupId,
                        entry.BackupName,
                        entry.Timestamp,
                        indexed.Order);

                    runs.Add(CreateRun(
                        format,
                        orphanStart,
                        indexed.Order,
                        entry.Timestamp,
                        LogRunStatus.Error,
                        null,
                        null,
                        runOrdinal++));
                }
            }
        }

        foreach (var open in openRuns.Values)
        {
            runs.Add(CreateRun(
                format,
                open,
                lastOrder,
                null,
                LogRunStatus.InProgress,
                null,
                null,
                runOrdinal++));
        }

        return runs;
    }

    private static InternalRun CreateRun(
        LogFormat format,
        OpenRun open,
        int lastOrder,
        DateTime? endTimestamp,
        LogRunStatus status,
        long? totalDurationMs,
        long? totalSizeBytes,
        int runOrdinal)
    {
        int boundedLastOrder = Math.Max(open.StartOrder, lastOrder);
        string runId = $"{format}:{open.StartOrder}:{runOrdinal}";
        string backupName = string.IsNullOrWhiteSpace(open.BackupName)
            ? $"Backup #{open.BackupId}"
            : open.BackupName;

        var summary = new LogRunSummary(
            runId,
            open.BackupId,
            backupName,
            format,
            open.StartTimestamp,
            endTimestamp,
            status,
            status == LogRunStatus.Completed ? totalDurationMs : null,
            status == LogRunStatus.Completed ? totalSizeBytes : null);

        return new InternalRun(summary, open.StartOrder, boundedLastOrder);
    }

    private static bool TryGetFileSignature(string filePath, out FileSignature signature)
    {
        signature = default;
        if (!File.Exists(filePath))
        {
            return false;
        }

        var info = new FileInfo(filePath);
        signature = new FileSignature(info.Length, info.LastWriteTimeUtc);
        return true;
    }

    private static bool HasSameSignatureSet(
        IReadOnlyDictionary<LogFormat, FileSignature> left,
        IReadOnlyDictionary<LogFormat, FileSignature> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var rightSignature))
            {
                return false;
            }

            if (!pair.Value.Equals(rightSignature))
            {
                return false;
            }
        }

        return true;
    }

    private sealed record FileCacheKey(DateOnly Date, LogFormat Format);

    private readonly record struct FileSignature(long Length, DateTime LastWriteTimeUtc);

    private sealed record FileCacheValue(FileSignature Signature, List<IndexedEntry> Entries);

    private sealed record IndexedEntry(LogEntry Entry, int Order);

    private sealed record OpenRun(int BackupId, string BackupName, DateTime StartTimestamp, int StartOrder);

    private sealed record InternalRun(LogRunSummary Summary, int StartOrder, int LastOrder)
    {
        public int BackupId => Summary.BackupId;

        public LogFormat Format => Summary.Format;
    }

    private sealed record DayIndexCacheValue(
        IReadOnlyDictionary<LogFormat, FileSignature> Signatures,
        IReadOnlyDictionary<LogFormat, IReadOnlyList<IndexedEntry>> EntriesByFormat,
        IReadOnlyDictionary<int, IReadOnlyList<LogRunSummary>> RunsByBackupId,
        IReadOnlyDictionary<string, InternalRun> RunsById,
        IReadOnlyList<LogJobSummary> JobSummaries)
    {
        public static DayIndexCacheValue Empty { get; } = new(
            new Dictionary<LogFormat, FileSignature>(),
            new Dictionary<LogFormat, IReadOnlyList<IndexedEntry>>(),
            new Dictionary<int, IReadOnlyList<LogRunSummary>>(),
            new Dictionary<string, InternalRun>(StringComparer.Ordinal),
            []);
    }
}
