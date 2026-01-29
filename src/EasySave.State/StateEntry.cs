namespace EasySave.State;

public class StateEntry
{
    public int BackupId { get; set; }
    public string BackupName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public BackupStatus Status { get; set; }
}
