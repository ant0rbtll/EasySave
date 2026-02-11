using System.Globalization;
using EasySave.Core;

namespace EasySave.Application;

internal static class LogFileNaming
{
    private const string DatePattern = "yyyy-MM-dd";

    /// <summary>
    /// Gets the expected file extension for a log format.
    /// </summary>
    /// <param name="format">Log format enum value.</param>
    /// <returns>Extension without leading dot.</returns>
    public static string GetFileExtension(LogFormat format)
    {
        return format switch
        {
            LogFormat.Json => "json",
            LogFormat.Xml => "xml",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported log format.")
        };
    }

    /// <summary>
    /// Builds the daily log filename for a date and a format.
    /// </summary>
    /// <param name="date">Target day.</param>
    /// <param name="format">Target log format.</param>
    /// <returns>File name including extension.</returns>
    public static string BuildFileName(DateOnly date, LogFormat format)
    {
        return $"{date.ToString(DatePattern, CultureInfo.InvariantCulture)}.{GetFileExtension(format)}";
    }

    /// <summary>
    /// Tries to parse a date from a log file path using the daily naming convention.
    /// </summary>
    /// <param name="filePath">Log file path.</param>
    /// <param name="date">Parsed date when successful.</param>
    /// <returns><c>true</c> when the date was parsed; otherwise <c>false</c>.</returns>
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
