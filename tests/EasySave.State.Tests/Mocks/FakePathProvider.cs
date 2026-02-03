using EasySave.Configuration;

public class TestPathProvider : IPathProvider
{
    private string statePath;

    private const string defaultStatePath = @"state.json";

    public TestPathProvider(string statePath = null)
    {
        this.statePath = statePath ?? defaultStatePath;
    }


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
