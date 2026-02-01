using System.Security.Cryptography.X509Certificates;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EasySave.Localization;

/// <summary>
/// The default implementation of the translation service
/// </summary>
public class LocalizationService : ILocalizationService
{

    private string culture;

    private Dictionary<string, string> allCultures = new()
    {
        { "fr", "config_locale_fr" },
        { "en", "config_locale_en" },
    };

    /// <summary>
    /// The initialisation of the service
    /// </summary>
    public LocalizationService()
    {
        culture = "fr";
    }

    /// <inheritdoc/>
    public string getCulture()
    {
        return culture;
    }

    /// <inheritdoc/>
    public void setCulture(string culture)
    {
        this.culture = culture;
    }

    /// <inheritdoc/>
    public Dictionary<string, string> getAllCultures()
    {
        return allCultures;
    }

    /// <inheritdoc/>
    public string translateTexte(string key)
    {
        //TODO link to conf
        var yaml = File.ReadAllText("../EasySave.Localization/Translations/translations." + culture + ".yaml");

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var data = deserializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(yaml);

        // Accès
        string code = data["translations"].TryGetValue(key, out var value) ? value : key;

        return code;
    }
}
