using System.Text.Encodings.Web;
using System.Text.Json;

namespace EasyLog;

public sealed class JsonLogFormatter : ILogFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string Format(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return JsonSerializer.Serialize(entry, Options);
    }
}