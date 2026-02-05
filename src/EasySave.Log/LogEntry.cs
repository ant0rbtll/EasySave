namespace EasySave.Log;

/// <summary>
/// Log model for EasySave v1.0 daily log.
/// Keep fields stable for backward compatibility.
/// </summary>
public record LogEntry(
    DateTime Timestamp,
    string BackupName,
    LogEventType EventType,
    string SourcePathUNC,
    string DestinationPathUNC,
    long FileSizeBytes,
    long TransferTimeMs
);
