namespace EasySave.Persistence;

/// <summary>
/// Represents user preferences and settings.
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// Gets or sets the application language/culture.
    /// </summary>
    public string Language { get; set; } = "fr";

    /// <summary>
    /// Gets or sets the custom log directory path (optional).
    /// </summary>
    public string? LogDirectory { get; set; }
}
