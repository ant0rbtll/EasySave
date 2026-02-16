namespace EasySave.Core;

/// <summary>
/// Represents a backup job configuration.
/// </summary>
public class BackupJob
{
    /// <summary>
    /// Gets or sets the unique identifier of the job.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the backup job.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source directory path.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination directory path.
    /// </summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of backup to perform.
    /// </summary>
    public BackupType Type { get; set; }

    /// <summary>
    /// Gets or sets the last execution date of the job (null if never executed).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? LastExecutionDate { get; set; }

    /// <summary>
    /// Gets or sets whether the job is currently active.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the runtime status of the job.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public BackupJobStatus Status { get; set; } = BackupJobStatus.Inactive;
}
