using EasySave.Configuration;

namespace EasySave.State.Tests.Mocks;

public class FakePathProvider(string? statePath = null) : IPathProvider
{
    private string statePath = statePath ?? Path.Combine(Path.GetTempPath(), $"state_{Guid.NewGuid():N}.json");


    /// <summary>
    /// Mock des chemins pour le fichier d'état temps réél
    /// </summary>
    #region MockPath
    public string GetStatePath()
    {
        return statePath;
    }

    public void SetStatePath(string path)
    {
        statePath = path;
    }

    public string GetJobsConfigPath()
    {
        return "unused_jobs.json";
    }

    public string GetDailyLogPath(DateTime date)
    {
        return $"unused_log_{date:yyyyMMdd}.log";
    }
    #endregion
}
