namespace EasySave.Backup;

/// <summary>
/// Defines a pluggable file encryption provider.
/// </summary>
public interface IEncryptionProvider
{
    /// <summary>
    /// Gets provider identifier used by configuration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Encrypts the specified file and returns execution details.
    /// </summary>
    /// <param name="filePath">Path to file to encrypt.</param>
    /// <param name="policy">Runtime encryption policy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Encryption execution result.</returns>
    Task<EncryptionResult> EncryptAsync(string filePath, EncryptionPolicy policy, CancellationToken cancellationToken = default);
}
