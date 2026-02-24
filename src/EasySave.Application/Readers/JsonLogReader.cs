using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Core;
using EasySave.Exceptions;
using EasySave.Log;

namespace EasySave.Application.Readers;

/// <summary>
/// Reads JSON daily logs.
/// </summary>
public sealed class JsonLogReader : LogReaderBase
{

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) }
    };

    /// <inheritdoc />
    public override LogFormat Format => LogFormat.Json;

    protected override IReadOnlyList<LogEntry> GetEntries(string log, string filePath)
    {
        try
        {
            return JsonSerializer.Deserialize<List<LogEntry>>(log, s_jsonOptions) ?? [];
        }
        catch (JsonException)
        {
            throw new InvalidLogFileException(filePath, "JSON");
        }
    }
}
