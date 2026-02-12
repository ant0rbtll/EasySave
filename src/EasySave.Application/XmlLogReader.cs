using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using EasySave.Core;
using EasySave.Log;

namespace EasySave.Application;

/// <summary>
/// Reads XML daily logs.
/// </summary>
public sealed class XmlLogReader : ILogReader
{
    private const long MaxLogFileSizeBytes = 50L * 1024 * 1024; // 50 MB

    /// <inheritdoc />
    public LogFormat Format => LogFormat.Xml;

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> ReadEntries(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path must not be empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Log file not found.", filePath);
        }

        string xml = FileReadResilience.ReadAllTextWithRetry(filePath, MaxLogFileSizeBytes);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return [];
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(xml, LoadOptions.None);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException($"Invalid XML log file '{filePath}'.", ex);
        }

        var root = document.Root;
        if (root is null)
        {
            return [];
        }

        if (!string.Equals(root.Name.LocalName, "Logs", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unexpected root element '{root.Name.LocalName}' in '{filePath}'.");
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

        throw new InvalidDataException($"Invalid timestamp '{raw}' in '{filePath}'.");
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

        throw new InvalidDataException($"Invalid event type '{raw}' in '{filePath}'.");
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

        throw new InvalidDataException($"Invalid numeric value '{raw}' for '{name}' in '{filePath}'.");
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
