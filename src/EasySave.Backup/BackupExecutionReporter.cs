using EasySave.Core;
using EasySave.Log;
using EasySave.State;

namespace EasySave.Backup;

/// <summary>
/// Reports backup execution progress to state storage and logs.
/// </summary>
public sealed class BackupExecutionReporter(IStateWriter stateWriter, ILogger logger)
{
    private readonly IStateWriter _stateWriter = stateWriter;
    private readonly ILogger _logger = logger;

    public void UpdateState(
        BackupJob job,
        BackupStatus status,
        int totalFiles,
        long totalSize,
        int remainingFiles,
        long remainingSize,
        int progress,
        string sourcePath,
        string destinationPath)
    {
        _stateWriter.Update(new StateEntry
        {
            BackupId = job.Id,
            BackupName = job.Name,
            Timestamp = DateTime.Now,
            Status = status,
            TotalFiles = totalFiles,
            TotalSizeBytes = totalSize,
            RemainingFiles = remainingFiles,
            RemainingSizeBytes = remainingSize,
            ProgressPercent = progress,
            CurrentSourcePath = sourcePath,
            CurrentDestinationPath = destinationPath
        });
    }

    public void Log(
        int backupId,
        string backupName,
        LogEventType eventType,
        string sourcePath,
        string destinationPath,
        long fileSizeBytes,
        long transferTimeMs,
        long encryptionTimeMs = 0)
    {
        try
        {
            _logger.Write(new LogEntry(
                DateTime.Now,
                backupId,
                backupName,
                eventType,
                sourcePath,
                destinationPath,
                fileSizeBytes,
                transferTimeMs,
                encryptionTimeMs
            ));
        }
        catch (Exception)
        {
            // Logging failures must not interrupt backup execution.
        }
    }

    public void LogPendingUserActions(BackupJob job, IBackupExecutionController executionController)
    {
        while (executionController.TryDequeueAction(out var actionKey))
        {
            Log(
                job.Id,
                job.Name,
                LogEventType.Action,
                actionKey,
                string.Empty,
                0,
                0);
        }
    }
}
