namespace EasySave.LogServer.Models;

/// <summary>
/// Server-side log entry enriched with client identification.
/// </summary>
public record EnrichedLogEntry(
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
    string? ClientName
);
