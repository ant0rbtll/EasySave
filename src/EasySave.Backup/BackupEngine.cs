using EasySave.Core;
using EasySave.System;
using EasySave.State;
using EasyLog;

namespace EasySave.Backup;

/// <summary>
/// Executes complete and differential backups.
/// </summary>
public class BackupEngine
{
    private readonly IFileSystem _fileSystem;
    private readonly ITransferService _transferService;
    private readonly IStateWriter _stateWriter;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the backup engine.
    /// </summary>
    /// <param name="fileSystem">File system management service.</param>
    /// <param name="transferService">File transfer service.</param>
    /// <param name="stateWriter">Backup state writer service.</param>
    /// <param name="logger">Logging service.</param>
    public BackupEngine(
        IFileSystem fileSystem,
        ITransferService transferService,
        IStateWriter stateWriter,
        ILogger logger)
    {
        _fileSystem = fileSystem;
        _transferService = transferService;
        _stateWriter = stateWriter;
        _logger = logger;
    }

    /// <summary>
    /// Executes a complete or differential backup.
    /// </summary>
    /// <param name="job">Backup job to execute.</param>
    /// <exception cref="NotSupportedException">The backup type is not supported.</exception>
    public void Execute(BackupJob job)
    {
        try
        {
        var files = _fileSystem.EnumerateFilesRecursive(job.Source).ToList();

        int totalFiles = files.Count;
        long totalSize = files.Sum(f => _fileSystem.GetFileSize(f));

        int remainingFiles = totalFiles;
        long remainingSize = totalSize;

        UpdateState(job, BackupStatus.Active, totalFiles, totalSize, remainingFiles, remainingSize, 0, "", "");

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(job.Source, file);
            var destinationFile = Path.Combine(job.Destination, relativePath);

            if (CanCopyFile(job.Type, file, destinationFile))
            {
                var destinationDir = Path.GetDirectoryName(destinationFile)!;

                if (!_fileSystem.DirectoryExists(destinationDir))
                {
                    _fileSystem.CreateDirectory(destinationDir);
                    Log(
                        job.Name,
                        LogEventType.CreateDirectory,
                        destinationDir,
                        destinationDir,
                        0,
                        0
                    );
                }

                TransferResult result = _transferService.TransferFile(file, destinationFile, true);

                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"File transfer failed from '{file}' to '{destinationFile}' with error code {result.ErrorCode}."
                    );
                }
                Log(
                    job.Name,
                    LogEventType.TransferFile,
                    file,
                    destinationFile,
                    result.FileSizeBytes,
                    result.TransferTimeMs
                );

                remainingFiles--;
                remainingSize -= result.FileSizeBytes;

                int progress = totalFiles > 0
                    ? (int)(100.0 * (totalFiles - remainingFiles) / totalFiles)
                    : 0;

                UpdateState(job, BackupStatus.Active, totalFiles, totalSize, remainingFiles, remainingSize, progress, file, destinationFile);
            }
        }

        UpdateState(job, BackupStatus.Done, totalFiles, totalSize, 0, 0, 100, "", "");
        _stateWriter.MarkInactive(job.Id);
        }
        catch (Exception)
        {
            UpdateState(job, BackupStatus.Error, 0, 0, 0, 0, 0, "", "");
            Log(
                job.Name,
                LogEventType.Error,
                "",
                "",
                0,
                0
            );
            throw;
        }
    }

    /// <summary>
    /// Determines if a file should be copied based on the backup type.
    /// </summary>
    /// <param name="type">Backup type (complete or differential).</param>
    /// <param name="sourceFile">Source file path.</param>
    /// <param name="destinationFile">Destination file path.</param>
    /// <returns>True if the file should be copied, otherwise false.</returns>
    /// <exception cref="NotSupportedException">The backup type is not supported.</exception>
    private bool CanCopyFile(BackupType type, string sourceFile, string destinationFile)
    {
        if (type == BackupType.Complete)
        {
            return true;
        }
        else if (type == BackupType.Differential)
        {
            var destinationDir = Path.GetDirectoryName(destinationFile)!;
            if (!_fileSystem.DirectoryExists(destinationDir))
            {
                return true;
            }
            if (!_fileSystem.FileExists(destinationFile))
            {
                return true;
            }

            var sourceSize = _fileSystem.GetFileSize(sourceFile);
            var destSize = _fileSystem.GetFileSize(destinationFile);

            return sourceSize != destSize;
        }
        else
        {
            throw new NotSupportedException($"Backup type {type} is not supported.");
        }
    }

    /// <summary>
    /// Updates the state of the current backup.
    /// </summary>
    /// <param name="job">Backup job.</param>
    /// <param name="status">Current backup status.</param>
    /// <param name="totalFiles">Total number of files.</param>
    /// <param name="totalSize">Total size in bytes.</param>
    /// <param name="remainingFiles">Number of remaining files.</param>
    /// <param name="remainingSize">Remaining size in bytes.</param>
    /// <param name="progress">Progress percentage.</param>
    /// <param name="src">Current source file path.</param>
    /// <param name="dst">Current destination file path.</param>
    private void UpdateState(
    BackupJob job,
    BackupStatus status,
    int totalFiles,
    long totalSize,
    int remainingFiles,
    long remainingSize,
    int progress,
    string src,
    string dst)
{
    _stateWriter.Update(new StateEntry {
        BackupId = job.Id,
        BackupName = job.Name,
        Timestamp = DateTime.Now,
        Status = status,
        TotalFiles = totalFiles,
        TotalSizeBytes = totalSize,
        RemainingFiles = remainingFiles,
        RemainingSizeBytes = remainingSize,
        ProgressPercent = progress,
        CurrentSourcePath = src,
        CurrentDestinationPath = dst
    });
}

    /// <summary>
    /// Logs a backup operation entry.
    /// </summary>
    /// <param name="backupName">Backup name.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="sourcePath">Source file path.</param>
    /// <param name="destinationPath">Destination file path.</param>
    /// <param name="fileSizeBytes">File size in bytes.</param>
    /// <param name="transferTimeMs">Transfer time in milliseconds.</param>
    private void Log(
        string backupName,
        LogEventType eventType,
        string sourcePath,
        string destinationPath,
        long fileSizeBytes,
        long transferTimeMs)
    {
        _logger.Write(new LogEntry(
            DateTime.Now,
            backupName,
            eventType,
            sourcePath,
            destinationPath,
            fileSizeBytes,
            transferTimeMs
        )
        );
    }


}
