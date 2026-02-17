namespace EasySave.Backup;

/// <summary>
/// Describes how one source file should be handled during a backup run.
/// </summary>
public sealed record BackupFilePlan(
    string SourceFile,
    string DestinationFile,
    bool IsPriority,
    bool ShouldCopy);
