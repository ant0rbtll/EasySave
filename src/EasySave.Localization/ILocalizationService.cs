namespace EasySave.Localization;

/// <summary>
/// The interface of the translation service
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Get the language of the app
    /// </summary>
    /// <returns>the language code</returns>
    string getCulture();

    /// <summary>
    /// Set the language of the app
    /// </summary>
    /// <param name="culture">The new language code</param>
    void setCulture(string culture);

    /// <summary>
    /// A list with all language possible
    /// </summary>
    /// <returns>The list of languages and codes</returns>
    Dictionary<string, string> getAllCultures();

    /// <summary>
    /// Translate a message with the current language
    /// </summary>
    /// <param name="key">The translation key</param>
    /// <returns>The message translated</returns>
    string translateTexte(string key);

}
