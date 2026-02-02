namespace EasySave.System;

public sealed record TransferResult(
    long FileSizeBytes,
    long TransferTimeMs,
    int ErrorCode
)
{
    public static class ErrorCodes
    {
        public const int None = 0;
        public const int InvalidSourcePath = -1;
        public const int InvalidDestinationPath = -2;
        public const int SourceNotFound = -3;
    }

    public bool IsSuccess => ErrorCode == 0;

    public static TransferResult InvalidSourcePath() =>
        new(FileSizeBytes: 0, TransferTimeMs: -1, ErrorCode: ErrorCodes.InvalidSourcePath);

    public static TransferResult InvalidDestinationPath() =>
        new(FileSizeBytes: 0, TransferTimeMs: -1, ErrorCode: ErrorCodes.InvalidDestinationPath);
}
