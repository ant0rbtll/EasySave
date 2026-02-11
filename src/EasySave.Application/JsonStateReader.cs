using System.Text.Json;
using EasySave.Configuration;
using EasySave.State;

namespace EasySave.Application;

/// <summary>
/// Reads state entries from the JSON state file.
/// </summary>
public sealed class JsonStateReader(IPathProvider pathProvider) : IStateReader
{
    private readonly IPathProvider _pathProvider = pathProvider;
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyDictionary<int, StateEntry> ReadEntries()
    {
        try
        {
            string path = _pathProvider.GetStatePath();

            if (!File.Exists(path))
            {
                return new Dictionary<int, StateEntry>();
            }

            string json = File.ReadAllText(path);
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
