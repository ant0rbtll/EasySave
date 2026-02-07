using System.Text.Encodings.Web;
using System.Text.Json;
using EasySave.Log;

namespace EasyLog;

/// <summary>
/// Formats log entries as indented JSON strings.
/// </summary>
public sealed class JsonLogFormatter : ILogFormatter, ILogFileLayout
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Formats a single log entry as a JSON object.
    /// </summary>
    /// <param name="entry">Entry to format.</param>
    /// <returns>Formatted JSON object string.</returns>
    public string Format(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return JsonSerializer.Serialize(entry, Options);
    }

    /// <summary>
    /// Gets the opening token of a JSON array log file.
    /// </summary>
    /// <returns>Array opening bracket.</returns>
    public string GetFileHeader() => "[";

    /// <summary>
    /// Gets the closing token of a JSON array log file.
    /// </summary>
    /// <returns>Array closing bracket.</returns>
    public string GetFileFooter() => "]";

    /// <summary>
    /// Gets the separator between two JSON entries.
    /// </summary>
    /// <returns>Comma separator.</returns>
    public string GetEntrySeparator() => ",";

    /// <summary>
    /// Gets indentation width used by the logger file writer.
    /// </summary>
    /// <returns>Indentation width in spaces.</returns>
    public int GetIndentSpaces() => 2;
}
