namespace EasySave.GUI.Models;

public class LogMonthPickerItemModel
{
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool HasLogs { get; set; }
    public bool IsSelected { get; set; }
}
