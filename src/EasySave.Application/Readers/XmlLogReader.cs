using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using EasySave.Core;
using EasySave.Exceptions;
using EasySave.Log;

namespace EasySave.Application.Readers;

/// <summary>
/// Reads XML daily logs.
/// </summary>
public sealed class XmlLogReader : LogReaderBase
{
    /// <inheritdoc />
    public override LogFormat Format => LogFormat.Xml;

    /// <inheritdoc />
    protected override IReadOnlyList<LogEntry> GetEntries(string log, string filePath)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(log, LoadOptions.None);
        }
        catch (XmlException)
        {
            throw new InvalidLogFileException(filePath, "XML");
        }

        var root = document.Root;
        if (root is null)
        {
            return [];
        }

        if (!string.Equals(root.Name.LocalName, "Logs", StringComparison.Ordinal))
        {
            throw new EasysaveDefaultException(Localization.LocalizationKey.error_unexpected_element, [root.Name.LocalName, filePath]);
        }

        var entries = new List<LogEntry>();
        foreach (var element in root.Elements("LogEntry"))
        {
            entries.Add(ParseEntry(element, filePath));
        }

        return entries;
    }

    /// <summary>
    /// Parses one <c>LogEntry</c> XML element into a typed model.
    /// </summary>
    /// <param name="element">Entry element to parse.</param>
    /// <param name="filePath">Source file path used for diagnostics.</param>
    /// <returns>Parsed log entry model.</returns>
    private static LogEntry ParseEntry(XElement element, string filePath)
    {
        DateTime timestamp = ParseTimestamp(element, filePath);
        string backupName = GetString(element, "BackupName");
        int backupId = (int)ParseLong(element, "BackupId", filePath);
        LogEventType eventType = ParseEventType(element, filePath);
        string sourcePath = GetString(element, "SourcePathUNC");
        string destinationPath = GetString(element, "DestinationPathUNC");
        long fileSizeBytes = ParseLong(element, "FileSizeBytes", filePath);
        long transferTimeMs = ParseLong(element, "TransferTimeMs", filePath);
        long encryptionTimeMs = ParseLong(element, "EncryptionTimeMs", filePath, fallback: 0);

        return new LogEntry(
            timestamp,
            backupId,
            backupName,
            eventType,
            sourcePath,
            destinationPath,
            fileSizeBytes,
            transferTimeMs,
            encryptionTimeMs);
    }

    /// <summary>
    /// Parses the timestamp value from one XML entry.
    /// </summary>
    /// <param name="element">Entry element to parse.</param>
    /// <param name="filePath">Source file path used for diagnostics.</param>
    /// <returns>Parsed timestamp.</returns>
    private static DateTime ParseTimestamp(XElement element, string filePath)
    {
        string raw = GetString(element, "Timestamp");
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value))
        {
            return value;
        }

        throw new EasysaveDefaultException(Localization.LocalizationKey.error_invalid_timestamp, [raw, filePath]);
    }

    /// <summary>
    /// Parses the event type value from one XML entry.
    /// </summary>
    /// <param name="element">Entry element to parse.</param>
    /// <param name="filePath">Source file path used for diagnostics.</param>
    /// <returns>Parsed event type.</returns>
    private static LogEventType ParseEventType(XElement element, string filePath)
    {
        string raw = GetString(element, "EventType");

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric))
        {
            return (LogEventType)numeric;
        }

        if (Enum.TryParse<LogEventType>(raw, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new EasysaveDefaultException(Localization.LocalizationKey.error_invalid_event_type, [raw, filePath]);
    }

    /// <summary>
    /// Parses a long integer field from one XML entry.
    /// </summary>
    /// <param name="element">Entry element to parse.</param>
    /// <param name="name">Field name.</param>
    /// <param name="filePath">Source file path used for diagnostics.</param>
    /// <param name="fallback">Value returned when the field is missing or empty.</param>
    /// <returns>Parsed numeric value or fallback.</returns>
    private static long ParseLong(XElement element, string name, string filePath, long fallback = 0)
    {
        string? raw = element.Element(name)?.Value;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
        {
            return parsed;
        }

        throw new EasysaveDefaultException(Localization.LocalizationKey.error_invalid_numeric_value, [raw, name, filePath]);
    }

    /// <summary>
    /// Gets a child element value as string.
    /// </summary>
    /// <param name="element">Parent element.</param>
    /// <param name="name">Child element name.</param>
    /// <returns>Child value or empty string when missing.</returns>
    private static string GetString(XElement element, string name)
    {
        return element.Element(name)?.Value ?? string.Empty;
    }
}
