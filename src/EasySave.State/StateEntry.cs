namespace EasySave.State;

public class StateEntry
{
    public int BackupId { get; set; }
    public string? BackupName { get; set; }
    public DateTime Timestamp { get; set; }
    public BackupStatus Status { get; set; }
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public int RemainingFiles { get; set; }
    public long RemainingSizeBytes { get; set; }
    public int ProgressPercent { get; set; }
    public string? CurrentSourcePath { get; set; }
    public string? CurrentDestinationPath { get; set; }
}