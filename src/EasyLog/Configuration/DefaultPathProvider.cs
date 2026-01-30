namespace EasyLog.Configuration;

public sealed class DefaultPathProvider : IPathProvider
{
    private readonly string _company;
    private readonly string _product;
    private readonly string _logsFolderName;

    public DefaultPathProvider(
        string company = "ProSoft",
        string product = "EasySave",
        string logsFolderName = "Logs")
    {
        _company = string.IsNullOrWhiteSpace(company) ? "ProSoft" : company.Trim();
        _product = string.IsNullOrWhiteSpace(product) ? "EasySave" : product.Trim();
        _logsFolderName = string.IsNullOrWhiteSpace(logsFolderName) ? "Logs" : logsFolderName.Trim();
    }

    public string GetDailyLogPath(DateTime date)
    {
        var baseDir = GetBaseDirectory();
        var dir = Path.Combine(baseDir, _company, _product, _logsFolderName);

        var file = $"{date:yyyy-MM-dd}.json";
        return Path.Combine(dir, file);
    }

    private static string GetBaseDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (!string.IsNullOrWhiteSpace(programData))
                return programData;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
            home = AppContext.BaseDirectory;

        return Path.Combine(home, ".local", "share");
    }
}