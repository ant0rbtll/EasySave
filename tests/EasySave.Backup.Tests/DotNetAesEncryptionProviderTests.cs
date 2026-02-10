using EasySave.Backup;

namespace EasySave.Backup.Tests;

public sealed class DotNetAesEncryptionProviderTests : IDisposable
{
    private readonly string _tempDirectory;

    public DotNetAesEncryptionProviderTests()
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

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task EncryptAsync_WithValidSettings_EncryptsInPlace()
    {
        var provider = new DotNetAesEncryptionProvider();
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);
        var filePath = Path.Combine(_tempDirectory, "data.txt");

        const string content = "This is a large enough payload to validate encryption.";
        await File.WriteAllTextAsync(filePath, content);
        var originalBytes = await File.ReadAllBytesAsync(filePath);

        var result = await provider.EncryptAsync(filePath, policy);

        Assert.True(result.IsSuccess);
        Assert.True(result.EncryptionTimeMs >= 0);
        Assert.True(File.Exists(filePath));

        var encryptedBytes = await File.ReadAllBytesAsync(filePath);
        Assert.NotEqual(originalBytes, encryptedBytes);
    }

    [Fact]
    public async Task EncryptAsync_SmallFile_ReturnsSuccess()
    {
        var provider = new DotNetAesEncryptionProvider();
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);
        var filePath = Path.Combine(_tempDirectory, "data.txt");

        await File.WriteAllTextAsync(filePath, "abc");

        var result = await provider.EncryptAsync(filePath, policy);

        Assert.True(result.IsSuccess);
        Assert.True(result.EncryptionTimeMs >= 0);
    }

    [Fact]
    public async Task EncryptAsync_WithMissingFile_ReturnsFailure()
    {
        var provider = new DotNetAesEncryptionProvider();
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);
        var filePath = Path.Combine(_tempDirectory, "missing.txt");

        var result = await provider.EncryptAsync(filePath, policy);

        Assert.False(result.IsSuccess);
        Assert.True(result.EncryptionTimeMs < 0);
    }

    [Fact]
    public async Task EncryptAsync_WithEmptyPath_ReturnsFailure()
    {
        var provider = new DotNetAesEncryptionProvider();
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);

        var result = await provider.EncryptAsync(string.Empty, policy);

        Assert.False(result.IsSuccess);
        Assert.True(result.EncryptionTimeMs < 0);
    }
}
