using EasySave.Backup;
using EasySave.Persistence;

namespace EasySave.Application;

/// <summary>
/// Builds runtime encryption policy from persisted user preferences.
/// </summary>
public sealed class UserPreferencesEncryptionPolicyProvider : IEncryptionPolicyProvider
{
    private readonly IUserPreferencesRepository _preferencesRepository;

    /// <summary>
    /// Initializes a new provider instance.
    /// </summary>
    /// <param name="preferencesRepository">Preferences repository.</param>
    public UserPreferencesEncryptionPolicyProvider(IUserPreferencesRepository preferencesRepository)
    {
        _preferencesRepository = preferencesRepository ?? throw new ArgumentNullException(nameof(preferencesRepository));
    }

    /// <inheritdoc />
    public EncryptionPolicy GetPolicy()
    {
        try
        {
            var preferences = _preferencesRepository.Load();
            return new EncryptionPolicy(
                preferences.EncryptedExtensions,
                preferences.EncryptionProvider,
                preferences.CryptoSoftExecutablePath);
        }
        catch (Exception)
        {
            // Backup execution must continue even if preferences are unavailable.
            return EncryptionPolicy.Disabled;
        }
    }
}
