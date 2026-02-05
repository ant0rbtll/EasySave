namespace EasySave.Core;

/// <summary>
/// Defines the type of backup operation.
/// </summary>
public enum BackupType
{
    /// <summary>
    /// Full backup: copies all files from source to destination.
    /// </summary>
    Complete,

    /// <summary>
    /// Differential backup: copies only files that differ from the destination.
    /// </summary>
    Differential
}
