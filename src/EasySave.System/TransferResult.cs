namespace EasySave.System;

public sealed record TransferResult(
    long FileSizeBytes,
    long TransferTimeMs,
    int ErrorCode
)
{
    public bool IsSuccess => ErrorCode == 0;
}