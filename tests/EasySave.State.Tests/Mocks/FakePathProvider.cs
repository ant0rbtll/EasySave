using EasySave.Configuration;

namespace EasySave.State.Tests.Mocks;

public class FakePathProvider(string? statePath = null) : IPathProvider
{
    private string _statePath = statePath ?? Path.Combine(Path.GetTempPath(), $"state_{Guid.NewGuid():N}.json");


    /// <summary>
    /// Mock des chemins pour le fichier d'état temps réel
    /// </summary>
    #region MockPath
    public string GetStatePath()
    {
        return _statePath;
    }

    public void SetStatePath(string path)
    {
        _statePath = path;
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
