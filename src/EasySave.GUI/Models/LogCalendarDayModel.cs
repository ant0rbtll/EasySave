using System;

namespace EasySave.GUI.Models;

public class LogCalendarDayModel
{
    public DateOnly Date { get; set; }
    public string DayText { get; set; } = string.Empty;
    public bool IsCurrentMonth { get; set; }
    public bool HasLogs { get; set; }
    public bool IsSelected { get; set; }
}
