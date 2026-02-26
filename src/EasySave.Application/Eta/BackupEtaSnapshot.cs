namespace EasySave.Application;

/// <summary>
/// Represents current ETA-related runtime metrics for a backup job.
/// </summary>
public readonly record struct BackupEtaSnapshot(
    TimeSpan? EstimatedRemainingTime,
    double? SmoothedThroughputBytesPerSecond);
