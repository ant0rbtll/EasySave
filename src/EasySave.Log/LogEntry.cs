namespace EasySave.Log;

/// <summary>
/// Log model for EasySave v1.0 daily log.
/// Keep fields stable for backward compatibility.
/// </summary>
public record LogEntry(
    DateTime Timestamp,
    int BackupId,
    string BackupName,
    LogEventType EventType,
    string SourcePathUNC,
    string DestinationPathUNC,
    long FileSizeBytes,
    long TransferTimeMs,
    long EncryptionTimeMs = 0
)
{
    /// <summary>
    /// Parameterless constructor kept for serializer compatibility.
    /// </summary>
    public LogEntry()
        : this(
            default,
            0,
            string.Empty,
            default,
            string.Empty,
            string.Empty,
            0,
            0,
            0)
    {
    }

    /// <summary>
    /// Backward-compatible constructor for older call sites that do not provide a backup id.
    /// </summary>
    public LogEntry(
        DateTime timestamp,
        string backupName,
        LogEventType eventType,
        string sourcePathUNC,
        string destinationPathUNC,
        long fileSizeBytes,
        long transferTimeMs,
        long encryptionTimeMs = 0)
        : this(
            timestamp,
            0,
            backupName,
            eventType,
            sourcePathUNC,
            destinationPathUNC,
            fileSizeBytes,
            transferTimeMs,
            encryptionTimeMs)
    {
    }
}
