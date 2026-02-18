using EasySave.Configuration;
using EasySave.Core;

namespace EasySave.LogServer.Services;

/// <summary>
/// Path provider for the centralized log server.
/// Only supports daily log path resolution.
/// </summary>
public class ServerPathProvider : IPathProvider
{
    private readonly string _logDirectory;

    public ServerPathProvider(string logDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public string GetDailyLogPath(DateTime date, LogFormat format = LogFormat.Json)
    {
        var extension = format == LogFormat.Xml ? "xml" : "json";
        var fileName = $"{date:yyyy-MM-dd}.{extension}";
        var fullPath = Path.Combine(_logDirectory, fileName);

        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        return fullPath;
    }

    public string GetStatePath() => throw new NotSupportedException();
    public string GetJobsConfigPath() => throw new NotSupportedException();
    public string GetUserPreferencesPath() => throw new NotSupportedException();
    public void SetLogDirectoryOverride(string? directory) { }
    public string ResolveLogsDirectory() => _logDirectory;
}
