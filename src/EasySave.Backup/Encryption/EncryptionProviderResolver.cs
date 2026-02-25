namespace EasySave.Backup;

/// <summary>
/// Default in-memory encryption provider resolver.
/// </summary>
public sealed class EncryptionProviderResolver : IEncryptionProviderResolver
{
    private readonly IReadOnlyDictionary<string, IEncryptionProvider> _providers;

    /// <summary>
    /// Initializes a new resolver from registered providers.
    /// </summary>
    /// <param name="providers">Available provider implementations.</param>
    public EncryptionProviderResolver(IEnumerable<IEncryptionProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        _providers = providers
            .Where(provider => !string.IsNullOrWhiteSpace(provider.Name))
            .GroupBy(provider => provider.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

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
