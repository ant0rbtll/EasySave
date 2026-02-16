using EasySave.Core;

namespace EasySave.Application;

/// <summary>
/// Represents runtime metadata displayed in the manage view.
/// </summary>
public readonly record struct BackupJobRuntimeState(
    int Id,
    BackupJobStatus Status,
    DateTime? LastExecutionDate,
    bool IsActive);
