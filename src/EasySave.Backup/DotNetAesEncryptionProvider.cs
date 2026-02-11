using System.Diagnostics;
using System.Security.Cryptography;
using EasySave.Core;

namespace EasySave.Backup;

/// <summary>
/// Built-in .NET provider that encrypts file content in-place using streaming AES.
/// A fixed key and IV are used so encrypted data can be decrypted later.
/// </summary>
public sealed class DotNetAesEncryptionProvider : IEncryptionProvider
{
    private const int EncryptionKeySizeBytes = 32;
    private const int IvSizeBytes = 16;
    private const int CopyBufferSize = 1024 * 128;
    private const string FixedEncryptionKeyBase64 = "jWfTkA2WzVd18XGN/FfYyQfQExx2Qx7XSE4aym0fNhA=";
    private const string FixedIvBase64 = "M2jnwD1Qd9nLGsw9YGjDMQ==";
    private static readonly byte[] FixedEncryptionKey = CreateFixedBytes(FixedEncryptionKeyBase64, EncryptionKeySizeBytes, nameof(FixedEncryptionKeyBase64));
    private static readonly byte[] FixedIv = CreateFixedBytes(FixedIvBase64, IvSizeBytes, nameof(FixedIvBase64));

    /// <inheritdoc />
    public string Name => EncryptionProviderNames.DotNet;

    /// <inheritdoc />
    public async Task<EncryptionResult> EncryptAsync(
        string filePath,
        EncryptionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return EncryptionResult.Failure(-1, -1, "File path is required.");
        }

        if (!File.Exists(filePath))
        {
            return EncryptionResult.Failure(-1, -1, "File not found.");
        }

        var stopwatch = Stopwatch.StartNew();
        var tempFilePath = BuildTempFilePath(filePath);

        try
        {
            await EncryptFileToTemporaryAsync(filePath, tempFilePath, FixedEncryptionKey, FixedIv, cancellationToken);
            File.Move(tempFilePath, filePath, overwrite: true);

            stopwatch.Stop();
            return EncryptionResult.Success(stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            CleanupTemporaryFile(tempFilePath);
            throw;
        }
        catch (CryptographicException ex)
        {
            stopwatch.Stop();
            CleanupTemporaryFile(tempFilePath);
            return EncryptionResult.Failure(GetNegativeElapsed(stopwatch), ex.HResult, ex.Message);
        }
        catch (IOException ex)
        {
            stopwatch.Stop();
            CleanupTemporaryFile(tempFilePath);
            return EncryptionResult.Failure(GetNegativeElapsed(stopwatch), ex.HResult, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            stopwatch.Stop();
            CleanupTemporaryFile(tempFilePath);
            return EncryptionResult.Failure(GetNegativeElapsed(stopwatch), ex.HResult, ex.Message);
        }
    }

    private static async Task EncryptFileToTemporaryAsync(
        string sourceFilePath,
        string encryptedFilePath,
        byte[] encryptionKey,
        byte[] iv,
        CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            sourceFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            CopyBufferSize,
            FileOptions.SequentialScan);

        await using var destination = new FileStream(
            encryptedFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            CopyBufferSize,
            FileOptions.SequentialScan);

        using var aes = Aes.Create();
        aes.KeySize = EncryptionKeySizeBytes * 8;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = encryptionKey;
        aes.IV = iv;

        using (var cryptoStream = new CryptoStream(destination, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        {
            await source.CopyToAsync(cryptoStream, CopyBufferSize, cancellationToken);
            cryptoStream.FlushFinalBlock();
        }

        await destination.FlushAsync(cancellationToken);
    }

    private static string BuildTempFilePath(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        string fileName = Path.GetFileName(filePath);
        return Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.enc.tmp");
    }

    private static void CleanupTemporaryFile(string tempFilePath)
    {
        try
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private static long GetNegativeElapsed(Stopwatch stopwatch)
    {
        return -(long)Math.Max(1, stopwatch.ElapsedMilliseconds);
    }

    private static byte[] CreateFixedBytes(string base64Value, int expectedLength, string valueName)
    {
        byte[] decoded;

        try
        {
            decoded = Convert.FromBase64String(base64Value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"{valueName} must be a valid Base64 string.", ex);
        }

        if (decoded.Length != expectedLength)
        {
            throw new InvalidOperationException($"{valueName} must decode to {expectedLength} bytes.");
        }

        return decoded;
    }
}
