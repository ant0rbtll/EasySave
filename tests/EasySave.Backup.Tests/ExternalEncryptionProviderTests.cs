using EasySave.Core;
using EasySave.Localization;

namespace EasySave.Backup.Tests;

public sealed class ExternalEncryptionProviderTests : IDisposable
{
    private readonly string _tempDirectory;

    public ExternalEncryptionProviderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "EasySave.Backup.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    [Fact]
    public async Task EncryptAsync_WhenExecutablePathIsNotConfigured_ReturnsFailure()
    {
        var runner = new TestRunner();
        var provider = CreateProvider(runner);
        var targetFile = CreateFile("data.txt", "payload");
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.External, null);

        var result = await provider.EncryptAsync(targetFile, policy);

        Assert.False(result.IsSuccess);
        Assert.True(result.EncryptionTimeMs < 0);
        Assert.Equal(LocalizationKey.error_invalid_argument.ToString(), result.ErrorMessage);
        Assert.Equal(0, runner.InvocationCount);
    }

    [Fact]
    public async Task EncryptAsync_WhenTargetFileDoesNotExist_ReturnsFailure()
    {
        var runner = new TestRunner();
        var provider = CreateProvider(runner);
        var executablePath = CreateFile("cryptosoft.exe", "fake binary");
        var missingFile = Path.Combine(_tempDirectory, "missing.txt");
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.External, executablePath);

        var result = await provider.EncryptAsync(missingFile, policy);

        Assert.False(result.IsSuccess);
        Assert.True(result.EncryptionTimeMs < 0);
        Assert.Equal(LocalizationKey.error_file_not_found.ToString(), result.ErrorMessage);
        Assert.Equal(0, runner.InvocationCount);
    }

    [Fact]
    public async Task EncryptAsync_WhenExecutableFileDoesNotExist_ReturnsFailure()
    {
        var runner = new TestRunner();
        var provider = CreateProvider(runner);
        var missingExecutable = Path.Combine(_tempDirectory, "missing-cryptosoft.exe");
        var targetFile = CreateFile("data.txt", "payload");
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.External, missingExecutable);

        var result = await provider.EncryptAsync(targetFile, policy);

        Assert.False(result.IsSuccess);
        Assert.True(result.EncryptionTimeMs < 0);
        Assert.Equal(LocalizationKey.error_file_not_found.ToString(), result.ErrorMessage);
        Assert.Equal(0, runner.InvocationCount);
    }

    [Fact]
    public async Task EncryptAsync_WhenRunnerFails_PropagatesFailureCode()
    {
        var runner = new TestRunner(ExternalCryptoProcessRunResult.Failure(42, "CryptoSoft failed"));
        var provider = CreateProvider(runner);
        var executablePath = CreateFile("cryptosoft.exe", "fake binary");
        var targetFile = CreateFile("data.txt", "payload");
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.External, executablePath);

        var result = await provider.EncryptAsync(targetFile, policy);

        Assert.False(result.IsSuccess);
        Assert.True(result.EncryptionTimeMs < 0);
        Assert.Equal(42, result.ErrorCode);
        Assert.Equal("CryptoSoft failed", result.ErrorMessage);
        Assert.Equal(1, runner.InvocationCount);
    }

    [Fact]
    public async Task EncryptAsync_WhenCalledConcurrently_EnforcesMonoInstance()
    {
        var runner = new BlockingRunner();
        var mutexName = $"EasySave_Test_CryptoSoft_Mutex_{Guid.NewGuid():N}";
        var providerA = new ExternalEncryptionProvider(runner, mutexName, TimeSpan.FromSeconds(3));
        var providerB = new ExternalEncryptionProvider(runner, mutexName, TimeSpan.FromSeconds(3));

        var executablePath = CreateFile("cryptosoft.exe", "fake binary");
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.External, executablePath);
        var fileA = CreateFile("a.txt", "A");
        var fileB = CreateFile("b.txt", "B");

        var firstTask = Task.Run(async () => await providerA.EncryptAsync(fileA, policy));
        Assert.True(runner.WaitUntilFirstInvocation(TimeSpan.FromSeconds(2)));

        var secondTask = Task.Run(async () => await providerB.EncryptAsync(fileB, policy));
        await Task.Delay(200);

        Assert.Equal(1, runner.InvocationCount);

        runner.Release();

        var resultA = await firstTask;
        var resultB = await secondTask;

        Assert.True(resultA.IsSuccess);
        Assert.True(resultB.IsSuccess);
        Assert.Equal(2, runner.InvocationCount);
        Assert.Equal(1, runner.MaxConcurrentInvocations);
    }

    [Fact]
    public async Task EncryptAsync_WhenMutexIsBusyAndTimeoutReached_ReturnsFailure()
    {
        var runner = new TestRunner();
        var mutexName = $"EasySave_Test_CryptoSoft_Mutex_{Guid.NewGuid():N}";
        var provider = new ExternalEncryptionProvider(runner, mutexName, TimeSpan.FromMilliseconds(150));
        var executablePath = CreateFile("cryptosoft.exe", "fake binary");
        var targetFile = CreateFile("data.txt", "payload");
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.External, executablePath);

        using var acquiredGate = new ManualResetEventSlim(false);
        using var releaseGate = new ManualResetEventSlim(false);

        var holderTask = Task.Run(() =>
        {
            using var heldMutex = new Mutex(false, mutexName);
            if (!heldMutex.WaitOne(TimeSpan.FromSeconds(1)))
            {
                return;
            }

            acquiredGate.Set();
            releaseGate.Wait(TimeSpan.FromSeconds(2));
            heldMutex.ReleaseMutex();
        });

        Assert.True(acquiredGate.Wait(TimeSpan.FromSeconds(1)));

        try
        {
            var result = await provider.EncryptAsync(targetFile, policy);

            Assert.False(result.IsSuccess);
            Assert.True(result.EncryptionTimeMs < 0);
            Assert.Equal(LocalizationKey.error_encryption_failed.ToString(), result.ErrorMessage);
            Assert.Equal(0, runner.InvocationCount);
        }
        finally
        {
            releaseGate.Set();
            await holderTask;
        }
    }

    [Fact]
    public async Task EncryptAsync_WhenExecutionTimeoutIsReached_ReturnsFailureAndReleasesMutex()
    {
        var runner = new BlockingRunner();
        var mutexName = $"EasySave_Test_CryptoSoft_Mutex_{Guid.NewGuid():N}";
        var provider = new ExternalEncryptionProvider(
            runner,
            mutexName,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromMilliseconds(120));

        var executablePath = CreateFile("cryptosoft.exe", "fake binary");
        var targetFile = CreateFile("data-timeout.txt", "payload");
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.External, executablePath);

        var result = await provider.EncryptAsync(targetFile, policy);

        Assert.False(result.IsSuccess);
        Assert.True(result.EncryptionTimeMs < 0);
        Assert.Equal(LocalizationKey.error_encryption_failed.ToString(), result.ErrorMessage);
        Assert.Equal(1, runner.InvocationCount);

        var mutexAcquiredFromOtherThread = await Task.Run(() =>
        {
            using var probeMutex = new Mutex(false, mutexName);
            var acquired = probeMutex.WaitOne(TimeSpan.FromMilliseconds(400));
            if (acquired)
            {
                probeMutex.ReleaseMutex();
            }

            return acquired;
        });

        Assert.True(mutexAcquiredFromOtherThread);
    }

    private ExternalEncryptionProvider CreateProvider(TestRunner runner)
    {
        return new ExternalEncryptionProvider(
            runner,
            $"EasySave_Test_CryptoSoft_Mutex_{Guid.NewGuid():N}",
            TimeSpan.FromSeconds(2));
    }

    private string CreateFile(string fileName, string content)
    {
        var path = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private sealed class TestRunner(ExternalCryptoProcessRunResult? result = null) : IExternalCryptoProcessRunner
    {
        private readonly ExternalCryptoProcessRunResult _result = result ?? ExternalCryptoProcessRunResult.Success();
        private int _invocationCount;

        public int InvocationCount => Volatile.Read(ref _invocationCount);

        public ExternalCryptoProcessRunResult Run(string executablePath, string filePath, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _invocationCount);
            return _result;
        }
    }

    private sealed class BlockingRunner : IExternalCryptoProcessRunner
    {
        private readonly ManualResetEventSlim _firstInvocationEntered = new(false);
        private readonly ManualResetEventSlim _releaseGate = new(false);
        private int _invocationCount;
        private int _activeInvocations;
        private int _maxConcurrentInvocations;

        public int InvocationCount => Volatile.Read(ref _invocationCount);
        public int MaxConcurrentInvocations => Volatile.Read(ref _maxConcurrentInvocations);

        public bool WaitUntilFirstInvocation(TimeSpan timeout)
        {
            return _firstInvocationEntered.Wait(timeout);
        }

        public void Release()
        {
            _releaseGate.Set();
        }

        public ExternalCryptoProcessRunResult Run(string executablePath, string filePath, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _invocationCount);

            var active = Interlocked.Increment(ref _activeInvocations);
            UpdateMaxConcurrent(active);

            _firstInvocationEntered.Set();
            _releaseGate.Wait(cancellationToken);

            Interlocked.Decrement(ref _activeInvocations);
            return ExternalCryptoProcessRunResult.Success();
        }

        private void UpdateMaxConcurrent(int active)
        {
            while (true)
            {
                var currentMax = Volatile.Read(ref _maxConcurrentInvocations);
                if (active <= currentMax)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _maxConcurrentInvocations, active, currentMax) == currentMax)
                {
                    return;
                }
            }
        }
    }
}
