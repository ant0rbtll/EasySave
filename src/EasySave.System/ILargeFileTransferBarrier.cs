namespace EasySave.System;

/// <summary>
/// Coordinates global concurrency for transfers of large files.
/// </summary>
public interface ILargeFileTransferBarrier
{
    /// <summary>
    /// Acquires an exclusive transfer slot when the file exceeds the configured threshold.
    /// Returns <see langword="null"/> when no slot is required.
    /// </summary>
    IDisposable? Acquire(
        long fileSizeBytes,
        long thresholdBytes,
        CancellationToken cancellationToken,
        Action? onWaitingForSlot = null);
}
