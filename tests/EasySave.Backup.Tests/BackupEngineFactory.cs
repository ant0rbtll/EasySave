using EasySave.Log;
using EasySave.State;
using EasySave.System;

namespace EasySave.Backup.Tests;

internal static class BackupEngineFactory
{
    public static BackupEngine Create(
        IFileSystem fileSystem,
        ITransferService transferService,
        IStateWriter stateWriter,
        ILogger logger,
        IEncryptionPolicyProvider? encryptionPolicyProvider = null,
        IEncryptionProviderResolver? encryptionProviderResolver = null,
        IBackupExecutionGuard? executionGuard = null,
        IPriorityFilesBarrier? priorityFilesBarrier = null,
        IBackupFilePlanner? filePlanner = null,
        IBackupExecutionController? executionController = null,
        ILargeFileTransferBarrier? largeFileTransferBarrier = null)
    {
        var reporter = new BackupExecutionReporter(stateWriter, logger);
        var runtimeGate = new BackupRuntimeGate(
            executionController ?? new NoOpBackupExecutionController(),
            priorityFilesBarrier ?? new NoOpPriorityFilesBarrier(),
            largeFileTransferBarrier ?? new NoOpLargeFileTransferBarrier(),
            executionGuard ?? new NoOpBackupExecutionGuard(),
            reporter);
        var fileExecutionService = new BackupFileExecutionService(
            fileSystem,
            transferService,
            encryptionProviderResolver ?? new NoOpEncryptionProviderResolver());

        return new BackupEngine(
            stateWriter,
            encryptionPolicyProvider ?? new NoOpEncryptionPolicyProvider(),
            filePlanner ?? new DefaultBackupFilePlanner(fileSystem),
            fileExecutionService,
            runtimeGate);
    }
}
