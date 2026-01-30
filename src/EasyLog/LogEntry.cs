using System.Text.Json.Serialization;

namespace EasyLog;

/// <summary>
/// Log model for EasySave v1.0 daily log.
/// Keep fields stable for backward compatibility.
/// </summary>
public sealed record LogEntry(
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("backupName")] string BackupName,
    [property: JsonPropertyName("eventType")] LogEventType EventType,
    [property: JsonPropertyName("sourcePathUNC")] string SourcePathUNC,
    [property: JsonPropertyName("destinationPathUNC")] string DestinationPathUNC,
    [property: JsonPropertyName("fileSizeBytes")] long FileSizeBytes,
    [property: JsonPropertyName("transferTimeMs")] long TransferTimeMs
);