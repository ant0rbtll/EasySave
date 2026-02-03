namespace EasyLog.Configuration;

public sealed class DefaultPathProvider : IPathProvider
{
    private readonly EasySave.Configuration.DefaultPathProvider _inner;

    public DefaultPathProvider(
        string company = "ProSoft",
        string product = "EasySave",
        string logsFolderName = "Logs",
        string stateFolderName = "State",
        string configFolderName = "Config")
    {
        _inner = new EasySave.Configuration.DefaultPathProvider();
    }

    public string GetDailyLogPath(DateTime date) => _inner.GetDailyLogPath(date);

    public string GetStatePath() => _inner.GetStatePath();

    public string GetJobsConfigPath() => _inner.GetJobsConfigPath();
}
