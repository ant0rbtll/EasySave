using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EasySave.Localization;

/// <summary>
/// The default implementation of the translation service
/// </summary>
public class LocalizationService : ILocalizationService
{
    public Dictionary<string, string> AllCultures { get; }

    private Dictionary<string, Dictionary<string, string>> _translationCache;

    /// <summary>
    /// The initialisation of the service
    /// </summary>
    public LocalizationService()
    {
        AllCultures = new()
        {
            { "fr", "config_locale_fr" },
            { "en", "config_locale_en" },
        };
        Culture = "fr";

    }

    public string Culture { get; set; }

    /// <inheritdoc/>
    public string TranslateTexte(string key)
    {
        //TODO link to conf
        if (_translationCache == null)
        {
            var yaml = File.ReadAllText("../EasySave.Localization/Translations/translations." + Culture + ".yaml");

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var data = deserializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(yaml);
            _translationCache = data;
        }

        // Accès
        string code = _translationCache["translations"].TryGetValue(key, out var value) ? value : key;

        return code;
    }
}
