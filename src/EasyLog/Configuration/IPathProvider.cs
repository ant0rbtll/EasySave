namespace EasyLog.Configuration;

public interface IPathProvider : EasySave.Configuration.IPathProvider
{
    /// <summary>
    /// Returns the full path to the daily log file for the given date (local filesystem path).
    /// Example (stored next to the executable): C:\Path\To\EasySave\Logs\2026-01-28.json
    /// </summary>
    string GetDailyLogFilePath(DateTime date);
}
