using System.Text.Json;
using EasySave.Configuration;

namespace EasySave.Persistence;

public class JsonUserPreferencesRepository : IUserPreferencesRepository
{
    private readonly IPathProvider _pathProvider;

    public JsonUserPreferencesRepository(IPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
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
        catch
        {
            return new UserPreferences();
        }
    }

    public void Save(UserPreferences preferences)
    {
        string path = _pathProvider.GetUserPreferencesPath();
        string json = JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
