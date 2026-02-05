namespace EasySave.System;

/// <summary>
/// Represents the result of a file transfer operation.
/// </summary>
/// <param name="FileSizeBytes">The size of the transferred file in bytes.</param>
/// <param name="TransferTimeMs">The transfer duration in milliseconds (negative on error).</param>
/// <param name="ErrorCode">The error code (0 indicates success).</param>
public sealed record TransferResult(
    long FileSizeBytes,
    long TransferTimeMs,
    int ErrorCode
)
{
    /// <summary>
    /// Defines standard error codes for transfer operations.
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>No error.</summary>
        public const int None = 0;

        /// <summary>The source path is invalid.</summary>
        public const int InvalidSourcePath = -1;

        /// <summary>The destination path is invalid.</summary>
        public const int InvalidDestinationPath = -2;

        /// <summary>The source file was not found.</summary>
        public const int SourceNotFound = -3;
    }

    /// <summary>
    /// Gets a value indicating whether the transfer completed successfully.
    /// </summary>
    public bool IsSuccess => ErrorCode == 0;

    /// <summary>
    /// Creates a result representing an invalid source path error.
    /// </summary>
    /// <returns>A failed <see cref="TransferResult"/>.</returns>
    public static TransferResult InvalidSourcePath() =>
        new(FileSizeBytes: 0, TransferTimeMs: -1, ErrorCode: ErrorCodes.InvalidSourcePath);

    /// <summary>
    /// Creates a result representing an invalid destination path error.
    /// </summary>
    /// <returns>A failed <see cref="TransferResult"/>.</returns>
    public static TransferResult InvalidDestinationPath() =>
        new(FileSizeBytes: 0, TransferTimeMs: -1, ErrorCode: ErrorCodes.InvalidDestinationPath);
}
