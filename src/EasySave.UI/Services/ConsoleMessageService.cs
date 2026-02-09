using EasySave.Localization;

namespace EasySave.UI.Services;

/// <summary>
/// Handles localized console output and standardized error rendering.
/// </summary>
internal class ConsoleMessageService(ILocalizationService localizationService, ErrorManager errorManager)
{
    private readonly ILocalizationService _localizationService = localizationService;
    private readonly ErrorManager _errorManager = errorManager;

    public void Write(LocalizationKey key, bool writeLine = true)
    {
        var message = _localizationService.TranslateText(key);
        if (writeLine)
        {
            Console.WriteLine(message);
            return;
        }

        Console.Write(message);
    }

    public void WriteWithParams(LocalizationKey key, string[] parameters, bool writeLine = true)
    {
        var message = _localizationService.TranslateTextWithParams(key, parameters);
        if (writeLine)
        {
            Console.WriteLine(message);
            return;
        }

        Console.Write(message);
    }

    public string Translate(LocalizationKey key)
    {
        return _localizationService.TranslateText(key);
    }

    public void ShowError(Exception exception)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Write(LocalizationKey.error);

        var messageKey = exception.Message;
        if (exception.Data.Contains("errorKey") && exception.Data["errorKey"] is string dataKey)
        {
            messageKey = dataKey;
        }

        if (_errorManager.TryGetMessage(messageKey, out var localizationKey))
        {
            WriteWithParams(
                localizationKey,
                exception.Data.Keys
                    .Cast<string>()
                    .Where(key => !string.Equals(key, "errorKey", StringComparison.Ordinal))
                    .OrderBy(key => key)
                    .Select(key => exception.Data[key]?.ToString() ?? string.Empty)
                    .ToArray());
        }
        else
        {
            Console.WriteLine(exception.Message);
        }

        Console.ResetColor();
    }
}
