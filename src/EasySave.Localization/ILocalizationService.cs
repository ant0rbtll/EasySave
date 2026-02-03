namespace EasySave.Localization;

/// <summary>
/// The interface of the translation service
/// </summary>
public interface ILocalizationService
{

    public string Culture { get; set; }
    public Dictionary<string, LocalizationKey> AllCultures { get; }

    /// <summary>
    /// Translate a message with the current language
    /// </summary>
    /// <param name="key">The translation key</param>
    /// <returns>The message translated</returns>
    string TranslateText(string key);
    string TranslateText(LocalizationKey key);

    /// <summary>
    /// Translate a message with the current language and inject parameters
    /// </summary>
    /// <param name="key">The translation key</param>
    /// <param name="parameters">Dictionary of parameters to inject (e.g., {"name", "value"})</param>
    /// <returns>The message translated with parameters replaced</returns>
    string TranslateText(LocalizationKey key, Dictionary<string, string> parameters);

}
