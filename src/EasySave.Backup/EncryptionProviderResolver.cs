namespace EasySave.Backup;

/// <summary>
/// Default in-memory encryption provider resolver.
/// </summary>
public sealed class EncryptionProviderResolver : IEncryptionProviderResolver
{
    private readonly IReadOnlyDictionary<string, IEncryptionProvider> _providers;

    /// <inheritdoc />
    public IEncryptionProvider? Resolve(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return null;
        }

        return _providers.TryGetValue(providerName.Trim(), out var provider)
            ? provider
            : null;
    }
}
