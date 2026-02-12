namespace EasySave.GUI.Models;

public class LogRunSummaryModel
{
    public string RunId { get; set; } = string.Empty;
    public int BackupId { get; set; }
    public string BackupName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TotalDuration { get; set; } = string.Empty;
    public string TotalSize { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public bool IsInProgress { get; set; }
    public bool IsError { get; set; }
}
