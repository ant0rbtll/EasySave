namespace EasySave.Persistence;

/// <summary>
/// Provides persistence operations for loading and saving <see cref="UserPreferences"/>.
/// </summary>
public interface IUserPreferencesRepository
{
    /// <summary>
    /// Loads the current user preferences from the underlying storage.
    /// </summary>
    /// <returns>The loaded <see cref="UserPreferences"/> instance.</returns>
    UserPreferences Load();

    /// <summary>
    /// Persists the specified user preferences to the underlying storage.
    /// </summary>
    /// <param name="preferences">The <see cref="UserPreferences"/> instance to save.</param>
    void Save(UserPreferences preferences);
}
