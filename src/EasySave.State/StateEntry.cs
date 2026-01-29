namespace EasySave.State;

public class StateEntry
{
    public int backupId;
    public string backupName;
    public DateTime timestamp;
    public BackupStatus status;
    public int totalFiles;
    public long totalSizeBytes;
    public int remainingFiles;
    public long remainingSizeBytes;
    public int progressPercent;
    public string currentSourcePath;
    public string currentDestinationPath;
}