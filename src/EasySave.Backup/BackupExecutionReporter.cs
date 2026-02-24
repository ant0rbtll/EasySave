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

    /// <summary>
    /// Persists the live execution state for one backup job.
    /// </summary>
    /// <param name="job">Backup job context.</param>
    /// <param name="status">Current backup status.</param>
    /// <param name="totalFiles">Total file count for this run.</param>
    /// <param name="totalSize">Total size in bytes for this run.</param>
    /// <param name="remainingFiles">Remaining file count.</param>
    /// <param name="remainingSize">Remaining size in bytes.</param>
    /// <param name="progress">Progress percentage.</param>
    /// <param name="sourcePath">Current source path context.</param>
    /// <param name="destinationPath">Current destination path context.</param>
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

    /// <summary>
    /// Writes one backup log entry.
    /// </summary>
    /// <param name="backupId">Backup identifier.</param>
    /// <param name="backupName">Backup name.</param>
    /// <param name="eventType">Log event type.</param>
    /// <param name="sourcePath">Source path context.</param>
    /// <param name="destinationPath">Destination path context.</param>
    /// <param name="fileSizeBytes">Transferred file size in bytes.</param>
    /// <param name="transferTimeMs">Transfer duration in milliseconds.</param>
    /// <param name="encryptionTimeMs">Encryption duration in milliseconds.</param>
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

    /// <summary>
    /// Flushes pending runtime user actions from the execution controller into logs.
    /// </summary>
    /// <param name="job">Backup job context.</param>
    /// <param name="executionController">Runtime execution controller.</param>
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
