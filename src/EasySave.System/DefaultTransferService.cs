using System.Diagnostics;

namespace EasySave.System;

public sealed class DefaultTransferService : ITransferService
{
    private readonly IFileSystem _fileSystem;

    public DefaultTransferService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public TransferResult TransferFile(string sourcePath, string destinationPath, bool overwrite)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return TransferResult.InvalidSourcePath();

        if (string.IsNullOrWhiteSpace(destinationPath))
            return TransferResult.InvalidDestinationPath();

        long sizeBytes = 0;
        var sw = Stopwatch.StartNew();

        try
        {
            if (!_fileSystem.FileExists(sourcePath))
                return new TransferResult(
                    FileSizeBytes: 0,
                    TransferTimeMs: -1,
                    ErrorCode: TransferResult.ErrorCodes.SourceNotFound);

            sizeBytes = _fileSystem.GetFileSize(sourcePath);

            // Ensure destination folder exists
            _fileSystem.EnsureDirectoryForFileExists(destinationPath);

            _fileSystem.CopyFile(sourcePath, destinationPath, overwrite);

            sw.Stop();
            return new TransferResult(
                FileSizeBytes: sizeBytes,
                TransferTimeMs: sw.ElapsedMilliseconds,
                ErrorCode: TransferResult.ErrorCodes.None);
        }
        catch (Exception ex)
        {
            sw.Stop();

            // Negative time on error (aligns with EasySave logging requirement)
            var negativeTime = -(long)Math.Max(1, sw.ElapsedMilliseconds);

            return new TransferResult(
                FileSizeBytes: sizeBytes,
                TransferTimeMs: negativeTime,
                ErrorCode: ex.HResult);
        }
    }
}
