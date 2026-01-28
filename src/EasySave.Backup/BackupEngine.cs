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
        // To be implemented
    }

    private void ExecuteCompleteBackup(SaveWork job)
    {
    }

    private void ExecuteDifferentialBackup(SaveWork job)
    {
    }

    private IEnumerable<string> GetAllFiles(string source)
    {
        return Enumerable.Empty<string>();
    }

}
