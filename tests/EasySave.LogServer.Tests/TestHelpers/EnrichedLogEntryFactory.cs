namespace EasySave.LogServer.Tests.TestHelpers;

internal static class EnrichedLogEntryFactory
{
    public static EnrichedLogEntry Create(
        DateTime? timestamp = null,
        int backupId = 1,
        string backupName = "Backup-1",
        string eventType = "FileTransferred",
        string sourcePath = @"C:\Source\file.txt",
        string destPath = @"D:\Dest\file.txt",
        long fileSizeBytes = 1024,
        long transferTimeMs = 50,
        long encryptionTimeMs = 10,
        string macAddress = "AA:BB:CC:DD:EE:FF",
        string? clientName = "Poste-1") => new(
        Timestamp: timestamp ?? new DateTime(2025, 3, 15, 10, 30, 0, DateTimeKind.Utc),
        BackupId: backupId,
        BackupName: backupName,
        EventType: eventType,
        SourcePathUNC: sourcePath,
        DestinationPathUNC: destPath,
        FileSizeBytes: fileSizeBytes,
        TransferTimeMs: transferTimeMs,
        EncryptionTimeMs: encryptionTimeMs,
        MacAddress: macAddress,
        ClientName: clientName
    );
}
