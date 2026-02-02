namespace EasySave.Core;

public class BackupJob
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public BackupType Type { get; set; }
}
