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
            return new TransferResult(0, -1, -1);

        if (string.IsNullOrWhiteSpace(destinationPath))
            return new TransferResult(0, -1, -1);

        long sizeBytes = 0;
        var sw = Stopwatch.StartNew();

        try
        {
            if (!_fileSystem.FileExists(sourcePath))
                return new TransferResult(0, -1, -2);

            sizeBytes = _fileSystem.GetFileSize(sourcePath);

            // Ensure destination folder exists
            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destDir) && !_fileSystem.DirectoryExists(destDir))
                _fileSystem.CreateDirectory(destDir);

            _fileSystem.CopyFile(sourcePath, destinationPath, overwrite);

            sw.Stop();
            return new TransferResult(sizeBytes, sw.ElapsedMilliseconds, 0);
        }
        catch (Exception ex)
        {
            sw.Stop();

            // Negative time on error (aligns with EasySave logging requirement)
            var negativeTime = -(long)Math.Max(1, sw.ElapsedMilliseconds);

            return new TransferResult(sizeBytes, negativeTime, ex.HResult);
        }
    }
}