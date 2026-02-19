namespace EasySave.System;

/// <summary>
/// No-op implementation used when no shared limiter is configured.
/// </summary>
public sealed class NoOpLargeFileTransferBarrier : ILargeFileTransferBarrier
{
    public LargeFileTransferAcquireResult TryAcquire(long fileSizeBytes, long thresholdBytes, out IDisposable? lease)
    {
        lease = null;
        return LargeFileTransferAcquireResult.NotRequired;
    }
}
