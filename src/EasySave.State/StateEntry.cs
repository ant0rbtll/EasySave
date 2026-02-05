namespace EasySave.State;

/// <summary>
/// Represents the real-time state of a single backup operation.
/// </summary>
public class StateEntry
{
    /// <summary>
    /// Gets or sets the identifier of the associated backup job.
    /// </summary>
    public int BackupId { get; set; }

    /// <summary>
    /// Gets or sets the name of the associated backup job.
    /// </summary>
    public string? BackupName { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last state update.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the current backup status.
    /// </summary>
    public BackupStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the total number of files to process.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the total size in bytes of all files to process.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of remaining files to process.
    /// </summary>
    public int RemainingFiles { get; set; }

    /// <summary>
    /// Gets or sets the remaining size in bytes to process.
    /// </summary>
    public long RemainingSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// Gets or sets the path of the file currently being processed (source).
    /// </summary>
    public string? CurrentSourcePath { get; set; }

    /// <summary>
    /// Gets or sets the path of the file currently being written (destination).
    /// </summary>
    public string? CurrentDestinationPath { get; set; }
}
