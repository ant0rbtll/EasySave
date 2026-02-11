namespace EasySave.Application;

internal static class FileReadResilience
{
    private static readonly TimeSpan[] s_retryDelays =
    [
        TimeSpan.FromMilliseconds(40),
        TimeSpan.FromMilliseconds(80)
    ];

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

    private static void EnsureFileSizeWithinLimit(string filePath, long maxFileSizeBytes)
    {
        var info = new FileInfo(filePath);
        if (info.Length > maxFileSizeBytes)
        {
            throw new InvalidDataException(
                $"File '{filePath}' is too large ({info.Length} bytes). Maximum allowed size is {maxFileSizeBytes} bytes.");
        }
    }
}
