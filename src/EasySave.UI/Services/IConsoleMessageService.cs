using EasySave.Localization;

namespace EasySave.UI.Services;

internal interface IConsoleMessageService
{
    void Write(LocalizationKey key, bool writeLine = true);
    void WriteWithParams(LocalizationKey key, string[] parameters, bool writeLine = true);
    string Translate(LocalizationKey key);
    void ShowError(Exception exception);
}
