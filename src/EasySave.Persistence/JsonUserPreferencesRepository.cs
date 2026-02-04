using System.Text.Json;
using EasySave.Configuration;

namespace EasySave.Persistence;

/// <summary>
/// JSON-based repository for user preferences persistence.
/// Returns default preferences when file is missing, empty, or corrupted.
/// </summary>
public class JsonUserPreferencesRepository : IUserPreferencesRepository
{
    private readonly IPathProvider _pathProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the JSON user preferences repository.
    /// </summary>
    /// <param name="pathProvider">Path provider for the preferences file location.</param>
    public JsonUserPreferencesRepository(IPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns default preferences if file does not exist, is empty, or contains invalid JSON.
    /// Catches specific exceptions (JsonException, IOException) and falls back to defaults.
    /// </remarks>
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
            return JsonSerializer.Deserialize<UserPreferences>(json, _jsonOptions) ?? new UserPreferences();
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

    /// <inheritdoc />
    /// <remarks>
    /// Creates the target directory if it does not exist before writing.
    /// Serializes preferences with camelCase property naming for consistency.
    /// </remarks>
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
