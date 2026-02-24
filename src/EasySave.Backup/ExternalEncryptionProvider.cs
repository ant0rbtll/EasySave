using System.Diagnostics;
using EasySave.Core;
using EasySave.Exceptions;

namespace EasySave.Backup;

/// <summary>
/// External provider that delegates encryption to the CryptoSoft executable.
/// Enforces mono-instance execution with a named cross-process mutex.
/// </summary>
public sealed class ExternalEncryptionProvider : IEncryptionProvider
{
    private static readonly string DefaultMutexName = OperatingSystem.IsWindows()
        ? @"Global\ProSoft_EasySave_CryptoSoft_SingleInstance"
        : "ProSoft_EasySave_CryptoSoft_SingleInstance";

    private static readonly TimeSpan DefaultMutexWaitTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultProcessExecutionTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MutexWaitPollInterval = TimeSpan.FromMilliseconds(100);

    private readonly IExternalCryptoProcessRunner _processRunner;
    private readonly string _mutexName;
    private readonly TimeSpan _mutexWaitTimeout;
    private readonly TimeSpan _processExecutionTimeout;

    /// <summary>
    /// Initializes a new instance using the default process runner and mutex settings.
    /// </summary>
    public ExternalEncryptionProvider()
        : this(new ExternalCryptoProcessRunner(), DefaultMutexName, DefaultMutexWaitTimeout, DefaultProcessExecutionTimeout)
    {
    }

    /// <summary>
    /// Initializes a new instance with explicit process runner and mutex settings.
    /// </summary>
    /// <param name="processRunner">Process runner used to invoke CryptoSoft.</param>
    /// <param name="mutexName">Named mutex used for cross-process mono-instance enforcement.</param>
    /// <param name="mutexWaitTimeout">Maximum time to wait for the mutex before failing.</param>
    public ExternalEncryptionProvider(
        IExternalCryptoProcessRunner processRunner,
        string mutexName,
        TimeSpan mutexWaitTimeout)
        : this(processRunner, mutexName, mutexWaitTimeout, DefaultProcessExecutionTimeout)
    {
    }

    /// <summary>
    /// Initializes a new instance with explicit dependencies.
    /// </summary>
    /// <param name="processRunner">Process runner used to invoke CryptoSoft.</param>
    /// <param name="mutexName">Named mutex used for cross-process mono-instance enforcement.</param>
    /// <param name="mutexWaitTimeout">Maximum time to wait for the mutex before failing.</param>
    /// <param name="processExecutionTimeout">Maximum execution time allowed for one CryptoSoft process.</param>
    public ExternalEncryptionProvider(
        IExternalCryptoProcessRunner processRunner,
        string mutexName,
        TimeSpan mutexWaitTimeout,
        TimeSpan processExecutionTimeout)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _mutexName = string.IsNullOrWhiteSpace(mutexName)
            ? throw new ArgumentException("Mutex name is required.", nameof(mutexName))
            : mutexName.Trim();
        _mutexWaitTimeout = mutexWaitTimeout <= TimeSpan.Zero
            ? throw new ArgumentOutOfRangeException(nameof(mutexWaitTimeout), "Mutex wait timeout must be greater than zero.")
            : mutexWaitTimeout;
        _processExecutionTimeout = processExecutionTimeout <= TimeSpan.Zero
            ? throw new ArgumentOutOfRangeException(nameof(processExecutionTimeout), "Process execution timeout must be greater than zero.")
            : processExecutionTimeout;
    }

    /// <inheritdoc />
    public string Name => EncryptionProviderNames.External;

    /// <inheritdoc />
    public Task<EncryptionResult> EncryptAsync(
        string filePath,
        EncryptionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        EasysaveDefaultException.ThrowIfNull(policy);

        if (string.IsNullOrWhiteSpace(policy.CryptoSoftExecutablePath))
        {
            return Task.FromResult(EncryptionResult.Failure(
                encryptionTimeMs: -1,
                errorCode: -1,
                errorMessage: "External provider selected but CryptoSoftExecutablePath is not configured."));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.FromResult(EncryptionResult.Failure(
                encryptionTimeMs: -1,
                errorCode: -1,
                errorMessage: "File path is required."));
        }

        var executablePath = policy.CryptoSoftExecutablePath.Trim();
        if (!File.Exists(executablePath))
        {
            return Task.FromResult(EncryptionResult.Failure(
                encryptionTimeMs: -1,
                errorCode: -1,
                errorMessage: $"CryptoSoft executable not found: {executablePath}"));
        }

        if (!File.Exists(filePath))
        {
            return Task.FromResult(EncryptionResult.Failure(
                encryptionTimeMs: -1,
                errorCode: -1,
                errorMessage: $"Target file not found: {filePath}"));
        }

        var stopwatch = Stopwatch.StartNew();
        bool mutexAcquired = false;
        using var mutex = new Mutex(initiallyOwned: false, name: _mutexName);

        try
        {
            mutexAcquired = WaitForMutex(mutex, _mutexWaitTimeout, cancellationToken);
            if (!mutexAcquired)
            {
                return Task.FromResult(EncryptionResult.Failure(
                    encryptionTimeMs: GetNegativeElapsed(stopwatch),
                    errorCode: -1,
                    errorMessage: $"Timeout while waiting for CryptoSoft mono-instance lock ({FormatTimeout(_mutexWaitTimeout)})."));
            }

            using var processTimeoutCts = new CancellationTokenSource();
            processTimeoutCts.CancelAfter(_processExecutionTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, processTimeoutCts.Token);

            ExternalCryptoProcessRunResult runResult;
            try
            {
                runResult = _processRunner.Run(executablePath, filePath, linkedCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && processTimeoutCts.IsCancellationRequested)
            {
                return Task.FromResult(EncryptionResult.Failure(
                    encryptionTimeMs: GetNegativeElapsed(stopwatch),
                    errorCode: -1,
                    errorMessage: $"CryptoSoft execution timed out after {FormatTimeout(_processExecutionTimeout)}."));
            }

            if (runResult.IsSuccess)
            {
                return Task.FromResult(EncryptionResult.Success(stopwatch.ElapsedMilliseconds));
            }

            return Task.FromResult(EncryptionResult.Failure(
                encryptionTimeMs: GetNegativeElapsed(stopwatch),
                errorCode: NormalizeErrorCode(runResult.ErrorCode),
                errorMessage: runResult.ErrorMessage ?? "CryptoSoft execution failed."));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Task.FromResult(EncryptionResult.Failure(
                encryptionTimeMs: GetNegativeElapsed(stopwatch),
                errorCode: NormalizeErrorCode(ex.HResult),
                errorMessage: ex.Message));
        }
        finally
        {
            if (mutexAcquired)
            {
                try
                {
                    mutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                    // Mutex ownership was lost unexpectedly; nothing else to do.
                }
            }
        }
    }

    private static bool WaitForMutex(Mutex mutex, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var remaining = timeout - elapsed.Elapsed;
            var slice = remaining > MutexWaitPollInterval ? MutexWaitPollInterval : remaining;
            if (slice <= TimeSpan.Zero)
            {
                break;
            }

            try
            {
                if (mutex.WaitOne(slice))
                {
                    return true;
                }
            }
            catch (AbandonedMutexException)
            {
                return true;
            }
        }

        return false;
    }

    private static long GetNegativeElapsed(Stopwatch stopwatch)
    {
        return -(long)Math.Max(1, stopwatch.ElapsedMilliseconds);
    }

    private static int NormalizeErrorCode(int errorCode)
    {
        return errorCode == 0 ? -1 : errorCode;
    }

    private static string FormatTimeout(TimeSpan timeout)
    {
        if (timeout.TotalSeconds >= 1)
        {
            return $"{Math.Ceiling(timeout.TotalSeconds):F0}s";
        }

        return $"{Math.Ceiling(timeout.TotalMilliseconds):F0}ms";
    }
}
