using EasySave.State;

namespace EasySave.Application;

/// <summary>
/// Computes ETA from a running average throughput measured on completed files.
/// </summary>
public sealed class BackupEtaEstimator : IBackupEtaEstimator
{
    private const double WarmupFileFraction = 0.07d;
    private const int MinimumWarmupFiles = 1;
    private const double WarmupSeconds = 15d;
    private const double MinDeltaSeconds = 0.05;
    private const double MinUsableThroughputBytesPerSecond = 1d;
    private const double MinUsableSecondsPerFile = 0.001d;
    private const double RegressionDeterminantEpsilon = 1e-9;
    private static readonly TimeSpan MaxEta = TimeSpan.FromDays(365);

    private readonly Dictionary<int, TrackedEtaState> _states = [];
    private readonly object _sync = new();

    /// <inheritdoc />
    public BackupEtaSnapshot UpdateEstimate(
        int jobId,
        BackupStatus status,
        int totalFiles,
        int remainingFiles,
        long totalSizeBytes,
        long remainingSizeBytes,
        DateTime observedAtUtc)
    {
        var utcNow = observedAtUtc.Kind == DateTimeKind.Utc
            ? observedAtUtc
            : observedAtUtc.ToUniversalTime();
        var clampedTotalFiles = Math.Max(0, totalFiles);
        var clampedRemainingFiles = Math.Max(0, remainingFiles);
        var completedFiles = Math.Max(0, clampedTotalFiles - clampedRemainingFiles);
        var clampedRemainingBytes = Math.Max(0L, remainingSizeBytes);
        var clampedTotalSizeBytes = Math.Max(0L, totalSizeBytes);

        lock (_sync)
        {
            if (!_states.TryGetValue(jobId, out var tracked))
            {
                tracked = new TrackedEtaState(
                    clampedTotalFiles,
                    completedFiles,
                    clampedRemainingBytes,
                    utcNow);
                _states[jobId] = tracked;
                return BuildSnapshot(
                    status,
                    clampedTotalFiles,
                    completedFiles,
                    clampedRemainingBytes,
                    clampedTotalSizeBytes,
                    tracked);
            }

            if (utcNow < tracked.LastObservationAtUtc)
            {
                utcNow = tracked.LastObservationAtUtc;
            }

            if (status == BackupStatus.Active
                && completedFiles > tracked.LastCompletedFiles
                && clampedRemainingBytes < tracked.LastRemainingBytes)
            {
                // Use elapsed time since last completed-file event to include
                // file read/write work and inter-file overhead.
                var elapsedSeconds = (utcNow - tracked.LastCompletionAtUtc).TotalSeconds;
                if (elapsedSeconds >= MinDeltaSeconds)
                {
                    var processedBytes = tracked.LastRemainingBytes - clampedRemainingBytes;
                    var completedFilesDelta = completedFiles - tracked.LastCompletedFiles;

                    if (completedFilesDelta > 0 && processedBytes >= 0)
                    {
                        tracked.CumulativeProcessedBytes += processedBytes;
                        tracked.CumulativeActiveSeconds += elapsedSeconds;
                        tracked.CumulativeProcessedFiles += completedFilesDelta;

                        // Online normal equations terms for:
                        // elapsedSeconds ~= a * processedBytes + b * completedFilesDelta
                        tracked.SumBytesSquared += processedBytes * (double)processedBytes;
                        tracked.SumFilesSquared += completedFilesDelta * (double)completedFilesDelta;
                        tracked.SumBytesFiles += processedBytes * (double)completedFilesDelta;
                        tracked.SumBytesTime += processedBytes * elapsedSeconds;
                        tracked.SumFilesTime += completedFilesDelta * elapsedSeconds;
                    }
                }

                tracked.LastCompletedFiles = completedFiles;
                tracked.LastCompletionAtUtc = utcNow;
            }

            tracked.TotalFiles = clampedTotalFiles;
            tracked.LastRemainingBytes = clampedRemainingBytes;
            tracked.LastObservationAtUtc = utcNow;

            return BuildSnapshot(
                status,
                clampedTotalFiles,
                completedFiles,
                clampedRemainingBytes,
                clampedTotalSizeBytes,
                tracked);
        }
    }

    /// <inheritdoc />
    public void Prune(IReadOnlyCollection<int> activeJobIds)
    {
        lock (_sync)
        {
            if (_states.Count == 0)
                return;

            if (activeJobIds.Count == 0)
            {
                _states.Clear();
                return;
            }

            var activeSet = activeJobIds is HashSet<int> hashSet
                ? hashSet
                : activeJobIds.ToHashSet();

            foreach (var jobId in _states.Keys.ToList())
            {
                if (!activeSet.Contains(jobId))
                {
                    _states.Remove(jobId);
                }
            }
        }
    }

    private static BackupEtaSnapshot BuildSnapshot(
        BackupStatus status,
        int totalFiles,
        int completedFiles,
        long remainingBytes,
        long totalSizeBytes,
        TrackedEtaState tracked)
    {
        var averageThroughputBytesPerSecond = tracked.CumulativeActiveSeconds > 0
            ? tracked.CumulativeProcessedBytes / tracked.CumulativeActiveSeconds
            : 0d;

        if (remainingBytes <= 0 || totalSizeBytes <= 0)
        {
            return new BackupEtaSnapshot(TimeSpan.Zero, averageThroughputBytesPerSecond > 0d
                ? averageThroughputBytesPerSecond
                : null);
        }

        if (status != BackupStatus.Active)
        {
            return new BackupEtaSnapshot(null, averageThroughputBytesPerSecond > 0d
                ? averageThroughputBytesPerSecond
                : null);
        }

        var warmupTarget = ComputeWarmupTarget(totalFiles);

        if (completedFiles < warmupTarget
            || tracked.CumulativeActiveSeconds < WarmupSeconds
            || averageThroughputBytesPerSecond < MinUsableThroughputBytesPerSecond)
        {
            return new BackupEtaSnapshot(null, null);
        }

        var remainingFiles = Math.Max(0, totalFiles - completedFiles);
        var etaSeconds = EstimateEtaSeconds(
            remainingBytes,
            remainingFiles,
            averageThroughputBytesPerSecond,
            tracked);
        var estimated = etaSeconds <= 0d
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(Math.Min(etaSeconds, MaxEta.TotalSeconds));

        return new BackupEtaSnapshot(estimated, averageThroughputBytesPerSecond);
    }

    private static double EstimateEtaSeconds(
        long remainingBytes,
        int remainingFiles,
        double averageThroughputBytesPerSecond,
        TrackedEtaState tracked)
    {
        var det = (tracked.SumBytesSquared * tracked.SumFilesSquared)
            - (tracked.SumBytesFiles * tracked.SumBytesFiles);

        if (Math.Abs(det) > RegressionDeterminantEpsilon)
        {
            var secondsPerByte =
                ((tracked.SumBytesTime * tracked.SumFilesSquared) - (tracked.SumFilesTime * tracked.SumBytesFiles)) / det;
            var secondsPerFile =
                ((tracked.SumBytesSquared * tracked.SumFilesTime) - (tracked.SumBytesFiles * tracked.SumBytesTime)) / det;

            // Clamp negative coefficients to keep ETA physically meaningful.
            secondsPerByte = Math.Max(0d, secondsPerByte);
            secondsPerFile = Math.Max(0d, secondsPerFile);

            if (secondsPerByte > 0d || secondsPerFile >= MinUsableSecondsPerFile)
            {
                return (secondsPerByte * remainingBytes) + (secondsPerFile * remainingFiles);
            }
        }

        return remainingBytes / averageThroughputBytesPerSecond;
    }

    private static int ComputeWarmupTarget(int totalFiles)
    {
        if (totalFiles <= 0)
            return MinimumWarmupFiles;

        var fractionalTarget = (int)Math.Ceiling(totalFiles * WarmupFileFraction);
        return Math.Max(MinimumWarmupFiles, fractionalTarget);
    }

    private sealed class TrackedEtaState(
        int totalFiles,
        int lastCompletedFiles,
        long lastRemainingBytes,
        DateTime lastObservationAtUtc)
    {
        public int TotalFiles { get; set; } = totalFiles;

        public int LastCompletedFiles { get; set; } = lastCompletedFiles;

        public long LastRemainingBytes { get; set; } = lastRemainingBytes;

        public DateTime LastObservationAtUtc { get; set; } = lastObservationAtUtc;

        public long CumulativeProcessedBytes { get; set; }

        public double CumulativeActiveSeconds { get; set; }

        public int CumulativeProcessedFiles { get; set; }

        public DateTime LastCompletionAtUtc { get; set; } = lastObservationAtUtc;

        public double SumBytesSquared { get; set; }

        public double SumFilesSquared { get; set; }

        public double SumBytesFiles { get; set; }

        public double SumBytesTime { get; set; }

        public double SumFilesTime { get; set; }
    }
}
