using EasySave.Core;

namespace EasySave.Backup;

/// <summary>
/// Placeholder provider for external CryptoSoft integration.
/// </summary>
public sealed class ExternalEncryptionProvider : IEncryptionProvider
{
    /// <inheritdoc />
    public string Name => EncryptionProviderNames.External;

    /// <inheritdoc />
    public Task<EncryptionResult> EncryptAsync(
        string filePath,
        EncryptionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (string.IsNullOrWhiteSpace(policy.CryptoSoftExecutablePath))
        {
            return Task.FromResult(EncryptionResult.Failure(
                encryptionTimeMs: -1,
                errorCode: -1,
                errorMessage: "External provider selected but CryptoSoftExecutablePath is not configured."));
        }

        return Task.FromResult(EncryptionResult.Failure(
            encryptionTimeMs: -1,
            errorCode: -1,
            errorMessage: "External encryption provider is not implemented yet."));
    }
}
