namespace EasySave.Localization;

/// <summary>
/// The interface of the translation service
/// </summary>
public interface ILocalizationService
{

    public string Culture { get; set; }
    public Dictionary<string, string> AllCultures { get; }

    /// <summary>
    /// Translate a message with the current language
    /// </summary>
    /// <param name="key">The translation key</param>
    /// <returns>The message translated</returns>
    string TranslateText(string key);

}
