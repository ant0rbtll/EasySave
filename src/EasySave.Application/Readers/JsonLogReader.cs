using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Core;
using EasySave.Log;

namespace EasySave.Application.Readers;

/// <summary>
/// Reads JSON daily logs.
/// </summary>
public sealed class JsonLogReader : ILogReader
{
    private const long MaxLogFileSizeBytes = 50L * 1024 * 1024; // 50 MB

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) }
    };

    /// <inheritdoc />
    public LogFormat Format => LogFormat.Json;

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

        string json = FileReadResilience.ReadAllTextWithRetry(filePath, MaxLogFileSizeBytes);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<LogEntry>>(json, s_jsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid JSON log file '{filePath}'.", ex);
        }
    }
}
