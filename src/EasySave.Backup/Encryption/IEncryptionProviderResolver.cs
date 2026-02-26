namespace EasySave.Backup;

/// <summary>
/// Resolves encryption providers by name.
/// </summary>
public interface IEncryptionProviderResolver
{
    /// <summary>
    /// Resolves a provider for the specified name.
    /// </summary>
    /// <param name="providerName">Configured provider name.</param>
    /// <returns>Matching provider or <see langword="null"/> when unavailable.</returns>
    IEncryptionProvider? Resolve(string providerName);
}
