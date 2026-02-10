using EasySave.Localization;

namespace EasySave.GUI.Helpers;

/// <summary>
/// Extracts localized error messages from exceptions enriched with errorKey and parameters.
/// </summary>
public static class ExceptionLocalizer
{
    public static string GetLocalizedMessage(Exception ex, ILocalizationService localization)
    {
        var errorKey = ex.Data.Contains("errorKey") && ex.Data["errorKey"] is string dataKey
            ? dataKey
            : ex.Message;

        if (Enum.TryParse<LocalizationKey>(errorKey, out var key))
        {
            var parameters = ex.Data.Keys
                .OfType<string>()
                .Where(k => !string.Equals(k, "errorKey", StringComparison.Ordinal))
                .OrderBy(k => k)
                .Select(k => ex.Data[k]?.ToString() ?? string.Empty)
                .ToArray();

            return localization.TranslateTextWithParams(key, parameters);
        }

        return localization.TranslateText(LocalizationKey.error_default);
    }
}
