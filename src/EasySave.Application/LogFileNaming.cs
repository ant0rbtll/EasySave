using System.Globalization;
using EasySave.Core;

namespace EasySave.Application;

internal static class LogFileNaming
{
    private const string DatePattern = "yyyy-MM-dd";

    public static string GetFileExtension(LogFormat format)
    {
        return format switch
        {
            LogFormat.Json => "json",
            LogFormat.Xml => "xml",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported log format.")
        };
    }

    public static string BuildFileName(DateOnly date, LogFormat format)
    {
        return $"{date.ToString(DatePattern, CultureInfo.InvariantCulture)}.{GetFileExtension(format)}";
    }

    public static bool TryParseDateFromFilePath(string filePath, out DateOnly date)
    {
        date = default;

        string? fileName = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return DateOnly.TryParseExact(
            fileName,
            DatePattern,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }
}
