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
        if (job.Type == BackupType.Complete)
        {
            ExecuteCompleteBackup(job);
        }
        else
        {
            ExecuteDifferentialBackup(job);
        }
    }

    private void ExecuteCompleteBackup(SaveWork job)
    {
        var files = GetAllFiles(job.Source);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(job.Source, file);
            var destinationFile = Path.Combine(job.Destination, relativePath);
            var destinationDir = Path.GetDirectoryName(destinationFile)!;

            if (!_fileSystem.DirectoryExists(destinationDir))
            {
                _fileSystem.CreateDirectory(destinationDir);
            }

            _transferService.TransferFile(file, destinationFile);
        }
    }

    private void ExecuteDifferentialBackup(SaveWork job)
    {
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
