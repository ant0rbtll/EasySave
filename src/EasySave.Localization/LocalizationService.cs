using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EasySave.Localization;

/// <summary>
/// The default implementation of the translation service
/// </summary>
public class LocalizationService : ILocalizationService
{
    public Dictionary<string, string> AllCultures { get; }

    private Dictionary<string, Dictionary<string, string>>? _translationCache;

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
    public string TranslateText(string key)
    {
        //TODO link to conf
        if (string.IsNullOrEmpty(key))
        {
            return key;
        }
        try
        {
            if (_translationCache == null)
            {
                var translationFilePath = Path.Combine("..", "EasySave.Localization", "Translations", $"translations.{Culture}.yaml");
                if (!File.Exists(translationFilePath))
                {
                    return key;
                }
                var yaml = File.ReadAllText(translationFilePath, Encoding.UTF8);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var data = deserializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(yaml);
                _translationCache = data;
            }

            if (_translationCache != null &&
                _translationCache.TryGetValue("translations", out var translations) &&
                translations != null &&
                translations.TryGetValue(key, out var value))
            {
                return value;
            }
            return key;

        }

        catch (IOException)
        {
            return key;
        }
        catch (Exception)
        {
            return key;
        }
    }
}
