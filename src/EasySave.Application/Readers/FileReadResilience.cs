using EasySave.Core.Exceptions;

namespace EasySave.Application.Readers;

/// <summary>
/// Provides resilient file read helpers used by readers in the application layer.
/// </summary>
internal static class FileReadResilience
{
    private static readonly TimeSpan[] s_retryDelays =
    [
        TimeSpan.FromMilliseconds(40),
        TimeSpan.FromMilliseconds(80)
    ];

    /// <summary>
    /// Reads a text file with a small retry policy for transient I/O errors and a size guard.
    /// </summary>
    /// <param name="filePath">Path of the file to read.</param>
    /// <param name="maxFileSizeBytes">Maximum accepted file size in bytes.</param>
    /// <returns>The full text content of the file.</returns>
    /// <exception cref="InvalidDataException">Thrown when file size exceeds the configured limit.</exception>
    public static string ReadAllTextWithRetry(string filePath, long maxFileSizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        EnsureFileSizeWithinLimit(filePath, maxFileSizeBytes);

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (IOException) when (attempt < s_retryDelays.Length)
            {
                Thread.Sleep(s_retryDelays[attempt]);
            }
        }
    }

    /// <summary>
    /// Ensures the target file does not exceed the configured maximum size.
    /// </summary>
    /// <param name="filePath">Path of the file to validate.</param>
    /// <param name="maxFileSizeBytes">Maximum accepted file size in bytes.</param>
    /// <exception cref="InvalidDataException">Thrown when file size exceeds the configured limit.</exception>
    private static void EnsureFileSizeWithinLimit(string filePath, long maxFileSizeBytes)
    {
        var info = new FileInfo(filePath);
        if (info.Length > maxFileSizeBytes)
        {
            throw new EasysaveDefaultException(
                $"File '{filePath}' is too large ({info.Length} bytes). Maximum allowed size is {maxFileSizeBytes} bytes.",
                ""
                );

            throw new InvalidDataException(
                $"File '{filePath}' is too large ({info.Length} bytes). Maximum allowed size is {maxFileSizeBytes} bytes.");
        }
    }
}
