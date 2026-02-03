using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EasySave.Localization;

/// <summary>
/// The default implementation of the translation service
/// </summary>
public class LocalizationService : ILocalizationService
{
    public Dictionary<string, LocalizationKey> AllCultures { get; }

    private Dictionary<string, Dictionary<string, string>>? _translationCache;

    /// <summary>
    /// The initialisation of the service
    /// </summary>
    public LocalizationService()
    {
        AllCultures = new()
        {
            { "fr", LocalizationKey.config_locale_fr },
            { "en", LocalizationKey.config_locale_en },
        };
        _culture = "fr";

    }

    private string _culture;
    public string Culture
    {
        get => _culture;
        set
        {
            if (_culture != value)
            {
                _culture = value;
                _translationCache = null; // Invalidate cache when culture changes
            }
        }
    }

    /// <inheritdoc/>
    public string TranslateText(LocalizationKey key)
    {
        return TranslateText(key.ToString());
    }

    /// <inheritdoc/>
    public string TranslateText(LocalizationKey key, Dictionary<string, string> parameters)
    {
        return TranslateText(key, parameters);
    }

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
                // Use assembly location to find translation files
                var assemblyLocation = Path.GetDirectoryName(typeof(LocalizationService).Assembly.Location);
                var translationFilePath = Path.Combine(assemblyLocation ?? ".", "Translations", $"translations.{Culture}.yaml");
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
