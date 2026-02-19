using System.Threading;

namespace EasySave.System;

/// <summary>
/// Global in-memory limiter that allows only one "large file" transfer at a time.
/// </summary>
public sealed class InMemoryLargeFileTransferBarrier : ILargeFileTransferBarrier, IDisposable
{
    private readonly SemaphoreSlim _largeTransferSemaphore = new(1, 1);
    private bool _disposed;

    public IDisposable? Acquire(
        long fileSizeBytes,
        long thresholdBytes,
        CancellationToken cancellationToken,
        Action? onWaitingForSlot = null)
    {
        if (_disposed
            || thresholdBytes <= 0
            || fileSizeBytes <= thresholdBytes)
        {
            return null;
        }

        if (!_largeTransferSemaphore.Wait(0, cancellationToken))
        {
            onWaitingForSlot?.Invoke();
            _largeTransferSemaphore.Wait(cancellationToken);
        }

        return new Releaser(_largeTransferSemaphore);
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
