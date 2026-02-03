namespace EasySave.Configuration
{
    public interface IPathProvider
    {
        string GetDailyLogPath(DateTime date);
        string GetStatePath();
        string GetJobsConfigPath();
    }
}