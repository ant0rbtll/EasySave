using EasySave.Core;
using EasySave.System;
using EasySave.State;
using EasyLog;

namespace EasySave.Backup;

public class BackupEngine
{
    private readonly IFileSystem _fileSystem;
    private readonly ITransferService _transferService;
    private readonly IStateWriter _stateWriter;
    private readonly ILogger _logger;

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

    public void Execute(SaveWork job)
    {
        var files = GetAllFiles(job.Source).ToList();

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
                }

                TransferResult result = _transferService.TransferFile(file, destinationFile);

                remainingFiles--;
                remainingSize -= result.FileSizeBytes;

                int progress = (int)(
                    100.0 * (totalFiles - remainingFiles) / totalFiles
                );

                UpdateState(job, BackupStatus.Active, totalFiles, totalSize, remainingFiles, remainingSize, progress, file, destinationFile);
            }
        }

        UpdateState(job, BackupStatus.Done, totalFiles, totalSize, 0, 0, 100, "", "");
    }
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

    private void UpdateState(
    SaveWork job,
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
        backupId = job.Id,
        backupName = job.Name,
        timestamp = DateTime.Now,
        status = status,
        totalFiles = totalFiles,
        totalSizeBytes = totalSize,
        remainingFiles = remainingFiles,
        remainingSizeBytes = remainingSize,
        progressPercent = progress,
        currentSourcePath = src,
        currentDestinationPath = dst
    });
}


    private IEnumerable<string> GetAllFiles(string source)
    {
        var files = _fileSystem.EnumerateFiles(source);

        foreach (var directory in _fileSystem.EnumerateDirectories(source))
        {
            files = files.Concat(GetAllFiles(directory));
        }

        return files;
    }


}
