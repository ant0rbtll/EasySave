namespace EasySave.Backup;

/// <summary>
/// Represents the outcome of a file encryption operation.
/// </summary>
public sealed record EncryptionResult(
    bool IsSuccess,
    long EncryptionTimeMs,
    int ErrorCode,
    string? ErrorMessage = null)
{
    /// <summary>
    /// Creates a successful encryption result.
    /// </summary>
    /// <param name="encryptionTimeMs">Operation duration in milliseconds.</param>
    /// <returns>A successful result instance.</returns>
    public static EncryptionResult Success(long encryptionTimeMs)
    {
        return new EncryptionResult(
            IsSuccess: true,
            EncryptionTimeMs: Math.Max(0, encryptionTimeMs),
            ErrorCode: 0,
            ErrorMessage: null);
    }

    /// <summary>
    /// Creates a failed encryption result.
    /// </summary>
    /// <param name="encryptionTimeMs">Operation duration in milliseconds (negative on error).</param>
    /// <param name="errorCode">Error code describing the failure.</param>
    /// <param name="errorMessage">Optional error details.</param>
    /// <returns>A failed result instance.</returns>
    public static EncryptionResult Failure(long encryptionTimeMs, int errorCode, string? errorMessage = null)
    {
        var failedTime = encryptionTimeMs < 0 ? encryptionTimeMs : -Math.Max(1, encryptionTimeMs);

        return new EncryptionResult(
            IsSuccess: false,
            EncryptionTimeMs: failedTime,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
    }
}
