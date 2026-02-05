namespace EasySave.Localization.Tests;

public class LocalizationServiceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesWithDefaultCulture()
    {
        // Act
        var service = new LocalizationService();

        // Assert
        Assert.Equal("fr", service.Culture);
    }

    [Fact]
    public void Constructor_InitializesAllCultures()
    {
        // Act
        var service = new LocalizationService();

        // Assert
        Assert.NotNull(service.AllCultures);
        Assert.Equal(2, service.AllCultures.Count);
        Assert.True(service.AllCultures.ContainsKey("fr"));
        Assert.True(service.AllCultures.ContainsKey("en"));
    }

    [Fact]
    public void Constructor_AllCulturesContainsCorrectLocalizationKeys()
    {
        // Act
        var service = new LocalizationService();

        // Assert
        Assert.Equal(LocalizationKey.config_locale_fr, service.AllCultures["fr"]);
        Assert.Equal(LocalizationKey.config_locale_en, service.AllCultures["en"]);
    }

    #endregion

    #region Culture Property Tests

    [Fact]
    public void Culture_SetNewValue_UpdatesCulture()
    {
        // Arrange
        var service = new LocalizationService();

        // Act
        service.Culture = "en";

        // Assert
        Assert.Equal("en", service.Culture);
    }

    [Fact]
    public void Culture_SetSameValue_DoesNotInvalidateCache()
    {
        // Arrange
        var service = new LocalizationService();
        service.Culture = "fr";

        // Act - Translate to populate cache
        var firstResult = service.TranslateText("menu");

        // Set same culture
        service.Culture = "fr";

        // Translate again
        var secondResult = service.TranslateText("menu");

        // Assert - Both should return the same translated value
        Assert.Equal(firstResult, secondResult);
    }

    [Fact]
    public void Culture_SetDifferentValue_InvalidatesCache()
    {
        // Arrange
        var service = new LocalizationService();

        // Act - Get French translation (back = "Retour" in FR)
        service.Culture = "fr";
        var frenchResult = service.TranslateText("back");

        // Switch to English (back = "Back" in EN)
        service.Culture = "en";
        var englishResult = service.TranslateText("back");

        // Assert - Should return different translations
        Assert.NotEqual(frenchResult, englishResult);
    }

    #endregion

    #region TranslateText(string) Tests

    [Fact]
    public void TranslateText_NullKey_ReturnsNull()
    {
        // Arrange
        var service = new LocalizationService();

        // Act
        var result = service.TranslateText((string)null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TranslateText_EmptyKey_ReturnsEmptyString()
    {
        // Arrange
        var service = new LocalizationService();

        // Act
        var result = service.TranslateText(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TranslateText_WhitespaceKey_ReturnsWhitespace()
    {
        // Arrange
        var service = new LocalizationService();

        // Act
        var result = service.TranslateText("   ");

        // Assert
        Assert.Equal("   ", result);
    }

    [Fact]
    public void TranslateText_ExistingKey_ReturnsTranslation()
    {
        // Arrange
        var service = new LocalizationService();
        service.Culture = "fr";

        // Act
        var result = service.TranslateText("menu");

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual("menu", result); // Should be translated, not the key itself
    }

    [Fact]
    public void TranslateText_NonExistentKey_ReturnsKey()
    {
        // Arrange
        var service = new LocalizationService();

        // Act
        var result = service.TranslateText("non_existent_key_xyz");

        // Assert
        Assert.Equal("non_existent_key_xyz", result);
    }

    [Fact]
    public void TranslateText_NonExistentCulture_ReturnsKey()
    {
        // Arrange
        var service = new LocalizationService();
        service.Culture = "xyz_invalid_culture";

        // Act
        var result = service.TranslateText("menu");

        // Assert
        Assert.Equal("menu", result);
    }

    [Fact]
    public void TranslateText_FrenchCulture_ReturnsFrenchTranslation()
    {
        // Arrange
        var service = new LocalizationService();
        service.Culture = "fr";

        // Act
        var result = service.TranslateText("back");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void TranslateText_EnglishCulture_ReturnsEnglishTranslation()
    {
        // Arrange
        var service = new LocalizationService();
        service.Culture = "en";

        // Act
        var result = service.TranslateText("back");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void TranslateText_SameKeyDifferentCultures_ReturnsDifferentTranslations()
    {
        // Arrange
        var serviceFr = new LocalizationService();
        serviceFr.Culture = "fr";

        var serviceEn = new LocalizationService();
        serviceEn.Culture = "en";

        // Act
        var frResult = serviceFr.TranslateText("back");
        var enResult = serviceEn.TranslateText("back");

        // Assert
        Assert.NotEqual(frResult, enResult);
    }

    [Fact]
    public void TranslateText_CalledMultipleTimes_UsesCachedTranslations()
    {
        // Arrange
        var service = new LocalizationService();
        service.Culture = "fr";

        // Act
        var result1 = service.TranslateText("menu");
        var result2 = service.TranslateText("menu");
        var result3 = service.TranslateText("back");

        // Assert - All calls should succeed and return consistent results
        Assert.Equal(result1, result2);
        Assert.NotNull(result3);
    }

    #endregion

    #region TranslateText(LocalizationKey) Tests

    [Fact]
    public void TranslateText_LocalizationKey_ReturnsTranslation()
    {
        // Arrange
        var service = new LocalizationService();
        service.Culture = "fr";

        // Act
        var result = service.TranslateText(LocalizationKey.menu);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual("menu", result); // Should be translated
    }

    [Fact]
    public void TranslateText_LocalizationKey_CallsStringOverload()
    {
        // Arrange
        var service = new LocalizationService();
        service.Culture = "fr";

        // Act
        var keyResult = service.TranslateText(LocalizationKey.back);
        var stringResult = service.TranslateText("back");

        // Assert - Both should return the same translation
        Assert.Equal(keyResult, stringResult);
    }

    [Fact]
    public void TranslateText_AllLocalizationKeys_ReturnNonEmptyStrings()
    {
        // Arrange
        var service = new LocalizationService();
        service.Culture = "fr";
        var allKeys = Enum.GetValues<LocalizationKey>();

        // Act & Assert
        Assert.All(allKeys, key =>
        {
            var result = service.TranslateText(key);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        });
    }

    [Fact]
    public void TranslateText_LocalizationKey_WithEnglishCulture_ReturnsEnglishTranslation()
    {
        // Arrange
        var service = new LocalizationService();
        service.Culture = "en";

        // Act
        var result = service.TranslateText(LocalizationKey.menu_quit);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    #endregion

    #region AllCultures Property Tests

    [Fact]
    public void AllCultures_IsNotNull()
    {
        // Arrange
        var service = new LocalizationService();

        // Act & Assert
        Assert.NotNull(service.AllCultures);
    }

    [Fact]
    public void AllCultures_ContainsSupportedCultures()
    {
        // Arrange
        var service = new LocalizationService();
        var expectedCultures = new[] { "fr", "en" };

        // Act & Assert
        foreach (var culture in expectedCultures)
        {
            Assert.True(service.AllCultures.ContainsKey(culture),
                $"Expected culture '{culture}' to be in AllCultures");
        }
    }

    [Fact]
    public void AllCultures_ValuesAreValidLocalizationKeys()
    {
        // Arrange
        var service = new LocalizationService();

        // Act & Assert
        foreach (var kvp in service.AllCultures)
        {
            Assert.True(Enum.IsDefined(typeof(LocalizationKey), kvp.Value),
                $"Value for culture '{kvp.Key}' should be a valid LocalizationKey");
        }
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public void TranslateText_AfterCultureChange_ReturnsCorrectTranslation()
    {
        // Arrange
        var service = new LocalizationService();

        // Act - Use "back" which has different translations (FR: "Retour", EN: "Back")
        service.Culture = "fr";
        var frResult = service.TranslateText("back");

        service.Culture = "en";
        var enResult = service.TranslateText("back");

        service.Culture = "fr";
        var frResultAgain = service.TranslateText("back");

        // Assert
        Assert.Equal(frResult, frResultAgain);
        Assert.NotEqual(frResult, enResult);
    }

    [Fact]
    public void TranslateText_SpecialCharactersInTranslation_ReturnsCorrectly()
    {
        // Arrange
        var service = new LocalizationService();
        service.Culture = "fr";

        // Act - Test with a key that might have special characters
        var result = service.TranslateText(LocalizationKey.input_escape_to_cancel);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void TranslateText_MultipleServices_IndependentCaches()
    {
        // Arrange
        var service1 = new LocalizationService();
        var service2 = new LocalizationService();

        // Act - Use "back" which has different translations (FR: "Retour", EN: "Back")
        service1.Culture = "fr";
        service2.Culture = "en";

        var result1 = service1.TranslateText("back");
        var result2 = service2.TranslateText("back");

        // Assert - Each service maintains its own cache
        Assert.NotEqual(result1, result2);
    }

    #endregion
}
