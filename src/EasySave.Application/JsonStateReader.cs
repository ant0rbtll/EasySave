using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using EasySave.Configuration;
using EasySave.State;

namespace EasySave.Application;

/// <summary>
/// Reads state entries from the JSON state file.
/// </summary>
public sealed class JsonStateReader(IPathProvider pathProvider) : IStateReader
{
    private const long MaxStateFileSizeBytes = 10L * 1024 * 1024; // 10 MB

    private readonly IPathProvider _pathProvider = pathProvider;
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true)
        }
    };

    /// <inheritdoc />
    public IReadOnlyDictionary<int, StateEntry> ReadEntries()
    {
        try
        {
            string path = _pathProvider.GetStatePath();

            if (!File.Exists(path))
            {
                return new Dictionary<int, StateEntry>();
            }

            string json = FileReadResilience.ReadAllTextWithRetry(path, MaxStateFileSizeBytes);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<int, StateEntry>();
            }

            return JsonSerializer.Deserialize<Dictionary<int, StateEntry>>(json, s_jsonOptions)
                   ?? new Dictionary<int, StateEntry>();
        }
        catch (JsonException)
        {
            return new Dictionary<int, StateEntry>();
        }
        catch (InvalidDataException)
        {
            return new Dictionary<int, StateEntry>();
        }
        catch (IOException)
        {
            return new Dictionary<int, StateEntry>();
        }
        catch (UnauthorizedAccessException)
        {
            return new Dictionary<int, StateEntry>();
        }
    }
}
