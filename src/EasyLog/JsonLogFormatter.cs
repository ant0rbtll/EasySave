using System.Text.Encodings.Web;
using System.Text.Json;
using EasySave.Log;

namespace EasyLog;

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
}

