namespace EasySave.GUI.Models;

public class LogDisplayModel
{
    public string Timestamp { get; set; } = string.Empty;
    public string BackupName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventTypeLabel { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string EncryptionTime { get; set; } = string.Empty;
}
