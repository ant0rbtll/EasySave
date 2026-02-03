namespace EasyLog.Configuration;

public interface IPathProvider : EasySave.Configuration.IPathProvider
{
    /// <summary>
    /// Returns the full path to the daily log file for the given date (local filesystem path).
    /// Example: C:\ProgramData\ProSoft\EasySave\Logs\2026-01-28.json
    /// </summary>
    new string GetDailyLogPath(DateTime date);
}
