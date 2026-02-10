namespace EasySave.Backup;

/// <summary>
/// Runtime encryption settings applied during backup execution.
/// </summary>
public sealed class EncryptionPolicy
{
    private readonly HashSet<string> _encryptedExtensions;

    /// <summary>
    /// Gets a policy that disables encryption.
    /// </summary>
    public static EncryptionPolicy Disabled { get; } = new([], EncryptionProviderNames.DotNet, null);

    /// <summary>
    /// Initializes a new policy instance.
    /// </summary>
    /// <param name="encryptedExtensions">File extensions to encrypt.</param>
    /// <param name="providerName">Configured provider name.</param>
    /// <param name="cryptoSoftExecutablePath">External provider executable path.</param>
    public EncryptionPolicy(
        IEnumerable<string>? encryptedExtensions,
        string? providerName,
        string? cryptoSoftExecutablePath)
    {
        _encryptedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (encryptedExtensions is not null)
        {
            foreach (var extension in encryptedExtensions)
            {
                var normalized = NormalizeExtension(extension);
                if (normalized is not null)
                {
                    _encryptedExtensions.Add(normalized);
                }
            }
        }

        ProviderName = string.IsNullOrWhiteSpace(providerName)
            ? EncryptionProviderNames.DotNet
            : providerName.Trim();

        CryptoSoftExecutablePath = string.IsNullOrWhiteSpace(cryptoSoftExecutablePath)
            ? null
            : cryptoSoftExecutablePath.Trim();
    }

    /// <summary>
    /// Gets configured provider name.
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Gets optional external executable path.
    /// </summary>
    public string? CryptoSoftExecutablePath { get; }

    /// <summary>
    /// Gets normalized encrypted extensions.
    /// </summary>
    public IReadOnlyCollection<string> EncryptedExtensions => _encryptedExtensions;

    /// <summary>
    /// Determines whether the specified file should be encrypted.
    /// </summary>
    /// <param name="filePath">Target file path.</param>
    /// <returns><see langword="true"/> when encryption is configured for this extension.</returns>
    public bool ShouldEncrypt(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || _encryptedExtensions.Count == 0)
        {
            return false;
        }

        var extension = NormalizeExtension(Path.GetExtension(filePath));
        if (extension is null)
        {
            return false;
        }

        return _encryptedExtensions.Contains(extension);
    }

    private static string? NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        var trimmed = extension.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (!trimmed.StartsWith(".", StringComparison.Ordinal))
        {
            trimmed = "." + trimmed;
        }

        return trimmed.ToLowerInvariant();
    }
}
