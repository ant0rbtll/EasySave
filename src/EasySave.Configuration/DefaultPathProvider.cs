namespace EasySave.Configuration;

public class DefaultPathProvider : IPathProvider
{
    public string GetDailyLogPath(DateTime date) => string.Empty;
    public string GetStatePath() => string.Empty;
    public string GetJobsConfigPath() => string.Empty;
}
