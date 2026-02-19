using System.Threading;

namespace EasySave.System;

/// <summary>
/// Global in-memory limiter that allows only one "large file" transfer at a time.
/// </summary>
public sealed class InMemoryLargeFileTransferBarrier : ILargeFileTransferBarrier, IDisposable
{
    private readonly SemaphoreSlim _largeTransferSemaphore = new(1, 1);
    private bool _disposed;

    public LargeFileTransferAcquireResult TryAcquire(
        long fileSizeBytes,
        long thresholdBytes,
        out IDisposable? lease)
    {
        if (_disposed
            || thresholdBytes <= 0
            || fileSizeBytes <= thresholdBytes)
        {
            lease = null;
            return LargeFileTransferAcquireResult.NotRequired;
        }

        if (!_largeTransferSemaphore.Wait(0))
        {
            lease = null;
            return LargeFileTransferAcquireResult.Busy;
        }

        lease = new Releaser(_largeTransferSemaphore);
        return LargeFileTransferAcquireResult.Acquired;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _largeTransferSemaphore.Dispose();
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        public void Dispose()
        {
            Interlocked.Exchange(ref _semaphore, null)?.Release();
        }
    }
}
