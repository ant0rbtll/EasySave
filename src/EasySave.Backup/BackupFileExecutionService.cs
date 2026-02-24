using EasySave.Core;
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

    public BackupFileTransferExecutionResult TransferAndEncrypt(BackupFilePlan plan, EncryptionPolicy policy)
    {
        var transferResult = _transferService.TransferFile(plan.SourceFile, plan.DestinationFile, overwrite: true);
        if (!transferResult.IsSuccess)
        {
            var message = $"File transfer failed from {plan.SourceFile} to {plan.DestinationFile} with error code {transferResult.ErrorCode}";
            var exception = new InvalidOperationException(message);
            exception.Data["errorKey"] = "error_file_transfer_failed";
            exception.Data["0_from"] = plan.SourceFile;
            exception.Data["1_destination"] = plan.DestinationFile;
            exception.Data["2_errorCode"] = transferResult.ErrorCode;
            throw exception;
        }

        var encryptionTimeMs = EncryptTransferredFileIfRequired(plan.DestinationFile, policy);
        return new BackupFileTransferExecutionResult(transferResult, encryptionTimeMs);
    }

    private long EncryptTransferredFileIfRequired(string destinationFile, EncryptionPolicy policy)
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
            var result = provider.EncryptAsync(destinationFile, policy).GetAwaiter().GetResult();
            if (result.IsSuccess)
            {
                return Math.Max(0, result.EncryptionTimeMs);
            }

            return result.EncryptionTimeMs < 0
                ? result.EncryptionTimeMs
                : -Math.Max(1, result.EncryptionTimeMs);
        }
        catch (Exception)
        {
            // Encryption failures must not interrupt backup execution.
            return -1;
        }
    }
}

public sealed record BackupDirectoryPreparationResult(bool Created, string DirectoryPath);

public sealed record BackupFileTransferExecutionResult(
    TransferResult TransferResult,
    long EncryptionTimeMs);
