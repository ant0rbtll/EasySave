namespace EasySave.Backup;

/// <summary>
/// Carries runtime options for one backup execution.
/// </summary>
public sealed record BackupExecutionContext
{
    public BackupExecutionContext(
        IReadOnlyCollection<string>? priorityExtensions = null,
        long parallelLargeFileThresholdBytes = 0)
    {
        PriorityExtensions = priorityExtensions ?? [];
        ParallelLargeFileThresholdBytes = Math.Max(0, parallelLargeFileThresholdBytes);
    }

    public IReadOnlyCollection<string> PriorityExtensions { get; }

    public long ParallelLargeFileThresholdBytes { get; }

    public static BackupExecutionContext Empty { get; } = new();
}
