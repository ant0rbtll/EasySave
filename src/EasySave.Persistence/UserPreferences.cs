using EasySave.Core;

namespace EasySave.Persistence;

/// <summary>
/// Represents user preferences and settings.
/// </summary>
public class UserPreferences
{
    private List<string>? _encryptedExtensions;
    private string _encryptionProvider = EncryptionProviderNames.DotNet;

    /// <summary>
    /// Gets or sets the application language/culture.
    /// </summary>
    public string Language { get; set; } = "fr";

    /// <summary>
    /// Gets or sets the custom log directory path (optional).
    /// </summary>
    public string? LogDirectory { get; set; }

    /// <summary>
    /// Gets or sets the log output format.
    /// </summary>
    public LogFormat LogFormat { get; set; } = LogFormat.Json;

    /// <summary>
    /// Gets or sets file extensions that should be encrypted after transfer.
    /// </summary>
    public List<string> EncryptedExtensions
    {
        get => _encryptedExtensions ??= [];
        set => _encryptedExtensions = value ?? [];
    }

    /// <summary>
    /// Gets or sets selected encryption provider name.
    /// </summary>
    public string EncryptionProvider
    {
        get => string.IsNullOrWhiteSpace(_encryptionProvider) ? EncryptionProviderNames.DotNet : _encryptionProvider;
        set => _encryptionProvider = string.IsNullOrWhiteSpace(value) ? EncryptionProviderNames.DotNet : value;
    }

    /// <summary>
    /// Gets or sets optional external CryptoSoft executable path.
    /// </summary>
    public string? CryptoSoftExecutablePath { get; set; }
}
