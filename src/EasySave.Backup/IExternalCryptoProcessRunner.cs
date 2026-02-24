namespace EasySave.Backup;

/// <summary>
/// Runs the external CryptoSoft process for a target file.
/// </summary>
public interface IExternalCryptoProcessRunner
{
    /// <summary>
    /// Executes CryptoSoft for the specified file.
    /// </summary>
    /// <param name="executablePath">Path to CryptoSoft executable.</param>
    /// <param name="filePath">Path to file to encrypt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result.</returns>
    ExternalCryptoProcessRunResult Run(string executablePath, string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a CryptoSoft process execution result.
/// </summary>
/// <param name="IsSuccess">True when the process ended successfully.</param>
/// <param name="ErrorCode">Error code when failed.</param>
/// <param name="ErrorMessage">Error message when failed.</param>
public readonly record struct ExternalCryptoProcessRunResult(
    bool IsSuccess,
    int ErrorCode,
    string? ErrorMessage)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ExternalCryptoProcessRunResult Success()
        => new(true, 0, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorCode">Failure code (0 is normalized to -1).</param>
    /// <param name="errorMessage">Failure details.</param>
    public static ExternalCryptoProcessRunResult Failure(int errorCode, string? errorMessage)
        => new(false, errorCode == 0 ? -1 : errorCode, errorMessage);
}
