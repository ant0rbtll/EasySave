using System.Text.Json;
using EasySave.Configuration;

namespace EasySave.Persistence;

public class JsonUserPreferencesRepository : IUserPreferencesRepository
{
    private readonly IPathProvider _pathProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonUserPreferencesRepository(IPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public UserPreferences Load()
    {
        string path = _pathProvider.GetUserPreferencesPath();

        if (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
            return new UserPreferences();
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
        }
        catch (JsonException)
        {
            // Corrupted or invalid JSON - return defaults
            return new UserPreferences();
        }
        catch (IOException)
        {
            // File read error (locked, concurrent access) - return defaults
            return new UserPreferences();
        }
    }

    public void Save(UserPreferences preferences)
    {
        string path = _pathProvider.GetUserPreferencesPath();
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(preferences, _jsonOptions);
        File.WriteAllText(path, json);
    }
}
