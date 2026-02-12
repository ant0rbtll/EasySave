namespace EasySave.GUI.Models;

public class LogJobSummaryModel
{
    public int BackupId { get; set; }
    public string BackupName { get; set; } = string.Empty;
    public int RunCount { get; set; }
}
