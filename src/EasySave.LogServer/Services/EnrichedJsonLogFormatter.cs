using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.LogServer.Models;
using EasyLog;

namespace EasySave.LogServer.Services;

/// <summary>
/// Formats enriched log entries as indented JSON strings.
/// Mirrors the style of <see cref="JsonLogFormatter"/> with additional MAC/client fields.
/// </summary>
public sealed class EnrichedJsonLogFormatter : ILogFileLayout
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) }
    };

    public string Format(EnrichedLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return JsonSerializer.Serialize(entry, Options);
    }

    public string GetFileHeader() => "[";
    public string GetFileFooter() => "]";
    public string GetEntrySeparator() => ",";
    public int GetIndentSpaces() => 2;
}
