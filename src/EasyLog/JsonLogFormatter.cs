using System.Text.Encodings.Web;
using System.Text.Json;
using EasySave.Log;

namespace EasyLog;

/// <summary>
/// Formats log entries as indented JSON strings.
/// </summary>
public sealed class JsonLogFormatter : ILogFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Format(EasySave.Log.LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return JsonSerializer.Serialize(entry, Options);
    }

    public string GetFileHeader() => "[";

    public string GetFileFooter() => "]";

    public string GetEntrySeparator() => ",";

    public int GetIndentSpaces() => 2;
}

