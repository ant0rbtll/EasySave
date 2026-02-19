namespace EasySave.Backup;

/// <summary>
/// Describes how one source file should be handled during a backup run.
/// </summary>
public sealed record BackupFilePlan(
    string SourceFile,
    string DestinationFile,
    long SourceFileSizeBytes,
    bool IsPriority,
    bool ShouldCopy);
