namespace EasySave.State;

public class StateEntry
{
    public int BackupId;
    public string? BackupName;
    public DateTime Timestamp;
    public BackupStatus Status;
    public int TotalFiles;
    public long TotalSizeBytes;
    public int RemainingFiles;
    public long RemainingSizeBytes;
    public int ProgressPercent;
    public string? CurrentSourcePath;
    public string? CurrentDestinationPath;
}