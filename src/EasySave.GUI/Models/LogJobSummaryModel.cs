using System.Collections.Generic;

namespace EasySave.GUI.Models;

public class LogJobSummaryModel
{
    public int BackupId { get; set; }
    public string BackupName { get; set; } = string.Empty;
    public int RunCount { get; set; }
    public string RunsSubtitle { get; set; } = string.Empty;
    public bool IsExpanded { get; set; }
    public List<LogRunSummaryModel> Runs { get; set; } = [];
}
