namespace EasyLog.Configuration;

public sealed class DefaultPathProvider : EasySave.Configuration.DefaultPathProvider, IPathProvider
{
    public DefaultPathProvider(
        string company = "ProSoft",
        string product = "EasySave",
        string logsFolderName = "Logs",
        string stateFolderName = "State",
        string configFolderName = "Config")
        : base(company, product, logsFolderName, stateFolderName, configFolderName)
    {
    }
}
