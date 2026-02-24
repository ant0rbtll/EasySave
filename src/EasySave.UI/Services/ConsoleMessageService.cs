using EasySave.Localization;

namespace EasySave.UI.Services;

/// <summary>
/// Handles localized console output and standardized error rendering.
/// </summary>
internal class ConsoleMessageService(
    ILocalizationService localizationService,
    ErrorManager errorManager,
    IConsoleAdapter consoleAdapter) : IConsoleMessageService
{
    private readonly ILocalizationService _localizationService = localizationService;
    private readonly ErrorManager _errorManager = errorManager;
    private readonly IConsoleAdapter _consoleAdapter = consoleAdapter;

    /// <inheritdoc />
    public void Write(LocalizationKey key, bool writeLine = true)
    {
        var message = _localizationService.TranslateText(key);
        InternalWrite(message, writeLine);

    }

    /// <inheritdoc />
    public void WriteWithParams(LocalizationKey key, string[] parameters, bool writeLine = true)
    {
        var message = _localizationService.TranslateTextWithParams(key, parameters);
        InternalWrite(message, writeLine);
    }

    /// <inheritdoc />
    public string Translate(LocalizationKey key)
    {
        return _localizationService.TranslateText(key);
    }

    /// <inheritdoc />
    public void ShowError(Exception exception)
    {
        _consoleAdapter.WriteLine();
        _consoleAdapter.SetForegroundColor(ConsoleColor.Red);
        Write(LocalizationKey.error);

        string exceptionMessage = _localizationService.GetTranslateTextException(exception);
        InternalWrite(exceptionMessage, true);
        _consoleAdapter.ResetColor();
    }

    private void InternalWrite(string message, bool writeLine)
    {
        if (writeLine)
        {
            _consoleAdapter.WriteLine(message);
            return;
        }

        _consoleAdapter.Write(message);
    }
}
