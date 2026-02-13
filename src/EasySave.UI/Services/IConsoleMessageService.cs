using EasySave.Localization;

namespace EasySave.UI.Services;

/// <summary>
/// Defines localized output and error display operations for the console UI.
/// </summary>
public interface IConsoleMessageService
{
    /// <summary>
    /// Writes a localized message.
    /// </summary>
    /// <param name="key">Localization key of the message.</param>
    /// <param name="writeLine">When true, appends a line terminator.</param>
    void Write(LocalizationKey key, bool writeLine = true);

    /// <summary>
    /// Writes a localized message with formatting parameters.
    /// </summary>
    /// <param name="key">Localization key of the message template.</param>
    /// <param name="parameters">Formatting parameters.</param>
    /// <param name="writeLine">When true, appends a line terminator.</param>
    void WriteWithParams(LocalizationKey key, string[] parameters, bool writeLine = true);

    /// <summary>
    /// Translates a localization key to plain text.
    /// </summary>
    /// <param name="key">Localization key to translate.</param>
    /// <returns>The translated text.</returns>
    string Translate(LocalizationKey key);

    /// <summary>
    /// Renders an exception in a user-friendly, localized way.
    /// </summary>
    /// <param name="exception">Exception to display.</param>
    void ShowError(Exception exception);
}
