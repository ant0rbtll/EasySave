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
        new(fileSizeBytes: 0, transferTimeMs: -1, errorCode: ErrorCodes.InvalidSourcePath);

    public static TransferResult InvalidDestinationPath() =>
        new(fileSizeBytes: 0, transferTimeMs: -1, errorCode: ErrorCodes.InvalidDestinationPath);
}
