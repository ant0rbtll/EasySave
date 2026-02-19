namespace EasySave.System;

/// <summary>
/// Result of a non-blocking large-file slot acquisition attempt.
/// </summary>
public enum LargeFileTransferAcquireResult
{
    NotRequired,
    Acquired,
    Busy
}

/// <summary>
/// Coordinates global concurrency for transfers of large files.
/// </summary>
public interface ILargeFileTransferBarrier
{
    /// <summary>
    /// Attempts to acquire an exclusive transfer slot without blocking.
    /// Returns an acquisition result and outputs a disposable lease when acquired.
    /// </summary>
    LargeFileTransferAcquireResult TryAcquire(
        long fileSizeBytes,
        long thresholdBytes,
        out IDisposable? lease);
}
