namespace EasySave.System;

/// <summary>
/// No-op implementation used when no shared limiter is configured.
/// </summary>
public sealed class NoOpLargeFileTransferBarrier : ILargeFileTransferBarrier
{
    public IDisposable? Acquire(long fileSizeBytes, long thresholdBytes, CancellationToken cancellationToken, Action? onWaitingForSlot = null) => null;
}
