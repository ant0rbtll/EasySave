namespace EasySave.State.Configuration.Paths
{
    public interface IPathProvider
    {
        string GetDailyLogPath(DateTime date);
        string GetStatePath();
        string GetJobsConfigPath();
    }
}