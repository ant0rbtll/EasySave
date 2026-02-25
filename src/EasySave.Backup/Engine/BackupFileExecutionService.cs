using EasySave.Core;
using EasySave.Exceptions;
using EasySave.Localization;
using EasySave.System;

namespace EasySave.Backup;

/// <summary>
/// Executes file-level backup operations: destination preparation, transfer and optional encryption.
/// </summary>
public sealed class BackupFileExecutionService(
    IFileSystem fileSystem,
    ITransferService transferService,
    IEncryptionProviderResolver encryptionProviderResolver)
{
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ITransferService _transferService = transferService;
    private readonly IEncryptionProviderResolver _encryptionProviderResolver = encryptionProviderResolver;

    /// <summary>
    /// Ensures the destination directory exists for a file to copy.
    /// </summary>
    /// <param name="destinationFile">Destination file path.</param>
    /// <returns>Information indicating whether a directory was created and which directory was targeted.</returns>
    public BackupDirectoryPreparationResult PrepareDestinationDirectory(string destinationFile)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationFile)!;
        if (_fileSystem.DirectoryExists(destinationDirectory))
        {
            return new BackupDirectoryPreparationResult(false, destinationDirectory);
        }

        _fileSystem.CreateDirectory(destinationDirectory);
        return new BackupDirectoryPreparationResult(true, destinationDirectory);
    }

    /// <summary>
    /// Executes file transfer and optional post-transfer encryption.
    /// </summary>
    /// <param name="plan">File execution plan.</param>
    /// <param name="policy">Runtime encryption policy.</param>
    /// <param name="cancellationToken">Cancellation token propagated to encryption provider.</param>
    /// <returns>Transfer and encryption execution result.</returns>
    /// <exception cref="EasysaveDefaultException">Thrown when file transfer fails.</exception>
    public BackupFileTransferExecutionResult TransferAndEncrypt(
        BackupFilePlan plan,
        EncryptionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var transferResult = _transferService.TransferFile(plan.SourceFile, plan.DestinationFile, overwrite: true);
        if (!transferResult.IsSuccess)
        {
            throw new EasysaveDefaultException(
                LocalizationKey.error_file_transfer_failed,
                [plan.SourceFile, plan.DestinationFile, transferResult.ErrorCode.ToString()]);
        }

        var encryptionTimeMs = EncryptTransferredFileIfRequired(plan.DestinationFile, policy, cancellationToken);
        return new BackupFileTransferExecutionResult(transferResult, encryptionTimeMs);
    }

    private long EncryptTransferredFileIfRequired(
        string destinationFile,
        EncryptionPolicy policy,
        CancellationToken cancellationToken)
    {
        if (!policy.ShouldEncrypt(destinationFile))
        {
            return 0;
        }

        var provider = _encryptionProviderResolver.Resolve(policy.ProviderName);
        if (provider is null)
        {
            return -1;
        }

        try
        {
            // Encryption is intentionally serialized/synchronous due to product constraints on the provider.
            var result = provider.EncryptAsync(destinationFile, policy, cancellationToken).GetAwaiter().GetResult();
            if (result.IsSuccess)
            {
                return Math.Max(0, result.EncryptionTimeMs);
            }

            return result.EncryptionTimeMs < 0
                ? result.EncryptionTimeMs
                : -Math.Max(1, result.EncryptionTimeMs);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Encryption failures must not interrupt backup execution.
            return -1;
        }
    }
}

/// <summary>
/// Result of destination directory preparation before file transfer.
/// </summary>
/// <param name="Created">Whether the directory was created by the operation.</param>
/// <param name="DirectoryPath">Destination directory path.</param>
public sealed record BackupDirectoryPreparationResult(bool Created, string DirectoryPath);

/// <summary>
/// Aggregates transfer and encryption execution outcomes for one file.
/// </summary>
/// <param name="TransferResult">Transfer result payload.</param>
/// <param name="EncryptionTimeMs">Measured encryption time in milliseconds.</param>
public sealed record BackupFileTransferExecutionResult(
    TransferResult TransferResult,
    long EncryptionTimeMs);
