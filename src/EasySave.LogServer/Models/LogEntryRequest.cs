namespace EasySave.LogServer.Models;

/// <summary>
/// Incoming DTO from EasySave clients containing a log entry and client MAC address.
/// </summary>
public record LogEntryRequest(
    DateTime Timestamp,
    int BackupId,
    string BackupName,
    string EventType,
    string SourcePathUNC,
    string DestinationPathUNC,
    long FileSizeBytes,
    long TransferTimeMs,
    long EncryptionTimeMs,
    string MacAddress,
    string? LogFormat = null
);
