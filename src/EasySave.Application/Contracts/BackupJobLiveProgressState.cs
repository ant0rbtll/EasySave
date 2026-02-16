using EasySave.Core;

namespace EasySave.Application;

/// <summary>
/// Represents real-time progress metadata used by the manage/progress views.
/// </summary>
public readonly record struct BackupJobLiveProgressState(
    int Id,
    string Name,
    BackupJobStatus Status,
    int ProgressPercent,
    int TotalFiles,
    long TotalSizeBytes,
    int RemainingFiles,
    long RemainingSizeBytes,
    string? CurrentSourcePath,
    string? CurrentDestinationPath,
    DateTime? LastUpdateAt);
