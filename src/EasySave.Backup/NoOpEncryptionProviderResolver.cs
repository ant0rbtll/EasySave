namespace EasySave.Backup;

/// <summary>
/// Resolver used when no encryption providers are configured.
/// </summary>
public sealed class NoOpEncryptionProviderResolver : IEncryptionProviderResolver
{
    /// <inheritdoc />
    public IEncryptionProvider? Resolve(string providerName) => null;
}
