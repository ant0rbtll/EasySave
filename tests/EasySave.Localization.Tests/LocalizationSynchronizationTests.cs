using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using EasySave.Localization;

namespace EasySave.Localization.Tests;

/// <summary>
/// Tests to verify synchronization between LocalizationKey enum and YAML translation files
/// </summary>
public class LocalizationSynchronizationTests
{
    private readonly string[] _supportedCultures = ["fr", "en"];

    /// <summary>
    /// Helper method to load keys from a YAML translation file
    /// </summary>
    private HashSet<string> LoadYamlKeys(string culture)
    {
        var translationFilePath = Path.Combine(
            "..", "..", "..", "..", "..", 
            "src", "EasySave.Localization", "Translations", 
            $"translations.{culture}.yaml"
        );

        if (!File.Exists(translationFilePath))
        {
            throw new FileNotFoundException($"Translation file not found: {translationFilePath}");
        }

        var yaml = File.ReadAllText(translationFilePath, Encoding.UTF8);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var data = deserializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(yaml);

        if (data != null && data.TryGetValue("translations", out var translations))
        {
            return translations.Keys.ToHashSet();
        }

        return new HashSet<string>();
    }

    /// <summary>
    /// Test that all enum keys exist in all YAML translation files
    /// </summary>
    [Fact]
    public void AllEnumKeys_ShouldExistInAllYamlFiles()
    {
        // Arrange
        var enumKeys = Enum.GetValues<LocalizationKey>()
            .Select(k => k.ToString())
            .ToHashSet();

        // Act & Assert
        foreach (var culture in _supportedCultures)
        {
            var yamlKeys = LoadYamlKeys(culture);

            var missingKeys = enumKeys.Except(yamlKeys).ToList();

            Assert.True(
                missingKeys.Count == 0,
                $"Culture '{culture}': The following keys exist in LocalizationKey enum but are missing in translations.{culture}.yaml:\n" +
                $"  - {string.Join("\n  - ", missingKeys)}"
            );
        }
    }

    /// <summary>
    /// Test that all YAML keys exist in the enum (no orphaned translations)
    /// </summary>
    [Fact]
    public void AllYamlKeys_ShouldExistInEnum()
    {
        // Arrange
        var enumKeys = Enum.GetValues<LocalizationKey>()
            .Select(k => k.ToString())
            .ToHashSet();

        // Act & Assert
        foreach (var culture in _supportedCultures)
        {
            var yamlKeys = LoadYamlKeys(culture);

            var orphanedKeys = yamlKeys.Except(enumKeys).ToList();

            Assert.True(
                orphanedKeys.Count == 0,
                $"Culture '{culture}': The following keys exist in translations.{culture}.yaml but are missing in LocalizationKey enum:\n" +
                $"  - {string.Join("\n  - ", orphanedKeys)}"
            );
        }
    }

    /// <summary>
    /// Test that all cultures have the same set of keys (consistency check)
    /// </summary>
    [Fact]
    public void AllCultures_ShouldHaveSameKeys()
    {
        // Arrange & Act
        var cultureSets = _supportedCultures.ToDictionary(
            culture => culture,
            culture => LoadYamlKeys(culture)
        );

        var firstCulture = _supportedCultures[0];
        var firstCultureKeys = cultureSets[firstCulture];

        // Assert
        foreach (var culture in _supportedCultures.Skip(1))
        {
            var cultureKeys = cultureSets[culture];

            var missingInCulture = firstCultureKeys.Except(cultureKeys).ToList();
            var extraInCulture = cultureKeys.Except(firstCultureKeys).ToList();

            Assert.True(
                missingInCulture.Count == 0,
                $"Culture '{culture}' is missing keys that exist in '{firstCulture}':\n" +
                $"  - {string.Join("\n  - ", missingInCulture)}"
            );

            Assert.True(
                extraInCulture.Count == 0,
                $"Culture '{culture}' has extra keys that don't exist in '{firstCulture}':\n" +
                $"  - {string.Join("\n  - ", extraInCulture)}"
            );
        }
    }

    /// <summary>
    /// Test that no translation value is empty
    /// </summary>
    [Fact]
    public void AllTranslations_ShouldNotBeEmpty()
    {
        foreach (var culture in _supportedCultures)
        {
            var translationFilePath = Path.Combine(
                "..", "..", "..", "..", "..",
                "src", "EasySave.Localization", "Translations",
                $"translations.{culture}.yaml"
            );

            var yaml = File.ReadAllText(translationFilePath, Encoding.UTF8);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var data = deserializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(yaml);

            if (data != null && data.TryGetValue("translations", out var translations))
            {
                var emptyTranslations = translations
                    .Where(kvp => string.IsNullOrWhiteSpace(kvp.Value))
                    .Select(kvp => kvp.Key)
                    .ToList();

                Assert.True(
                    emptyTranslations.Count == 0,
                    $"Culture '{culture}' has empty translations for keys:\n" +
                    $"  - {string.Join("\n  - ", emptyTranslations)}"
                );
            }
        }
    }
}
