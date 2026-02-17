using EasySave.Core;
using EasySave.Log;
using EasySave.System;
using EasySave.State;
using System.Diagnostics;
using System.Threading;

namespace EasySave.Backup;

/// <summary>
/// Executes complete and differential backups.
/// </summary>
/// <remarks>
/// Initializes a new instance of the backup engine.
/// </remarks>
/// <param name="stateWriter">Backup state writer service.</param>
/// <param name="encryptionPolicyProvider">Encryption policy provider.</param>
/// <param name="filePlanner">Backup file planner.</param>
/// <param name="fileExecutionService">File execution service.</param>
/// <param name="runtimeGate">Runtime gate for pause/stop and barriers.</param>
public class BackupEngine(
    IStateWriter stateWriter,
    IEncryptionPolicyProvider encryptionPolicyProvider,
    IBackupFilePlanner filePlanner,
    BackupFileExecutionService fileExecutionService,
    BackupRuntimeGate runtimeGate) : IBackupEngine
{
    private readonly IStateWriter _stateWriter = stateWriter;
    private readonly IEncryptionPolicyProvider _encryptionPolicyProvider = encryptionPolicyProvider;
    private readonly IBackupFilePlanner _filePlanner = filePlanner;
    private readonly BackupFileExecutionService _fileExecutionService = fileExecutionService;
    private readonly BackupRuntimeGate _runtimeGate = runtimeGate;

    /// <summary>
    /// Executes a complete or differential backup.
    /// </summary>
    /// <param name="job">Backup job to execute.</param>
    /// <exception cref="NotSupportedException">The backup type is not supported.</exception>
    #region Execute
    public Task Execute(
        BackupJob job,
        CancellationToken cancellationToken = default,
        BackupExecutionContext? executionContext = null)
    {
        return Task.Run(
            () => ExecuteCore(job, cancellationToken, executionContext ?? BackupExecutionContext.Empty),
            cancellationToken);
    }

    private void ExecuteCore(BackupJob job, CancellationToken cancellationToken, BackupExecutionContext executionContext)
    {
        long totalDurationMs = 0;
        Stopwatch? backupLoopTimer = null;
        _runtimeGate.BeginJob(job.Id);
        bool barrierRegistered = false;
        try
        {
            var plannedFiles = _filePlanner.BuildPlans(job, executionContext.PriorityExtensions);
            var parallelLargeFileThresholdBytes = Math.Max(0, executionContext.ParallelLargeFileThresholdBytes);
            var encryptionPolicy = _encryptionPolicyProvider.GetPolicy() ?? EncryptionPolicy.Disabled;

            int totalFiles = plannedFiles.Count;
            long totalSize = plannedFiles.Sum(f => f.SourceFileSizeBytes);

            int remainingFiles = totalFiles;
            long remainingSize = totalSize;

            _runtimeGate.UpdateState(job, BackupStatus.Active, totalFiles, totalSize, remainingFiles, remainingSize, 0, "", "");
            _runtimeGate.Log(job.Id, job.Name, LogEventType.StartBackup, "", "", totalSize, 0);

            _runtimeGate.RegisterPriorityJob(job.Id, plannedFiles.Count(f => f.ShouldCopy && f.IsPriority));
            barrierRegistered = true;

            backupLoopTimer = Stopwatch.StartNew();
            foreach (var planned in plannedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _runtimeGate.LogPendingUserActions(job);

                _runtimeGate.WaitForRuntimeControlAndSyncState(
                    job,
                    totalFiles,
                    totalSize,
                    remainingFiles,
                    remainingSize,
                    planned.SourceFile,
                    planned.DestinationFile);


                if (!planned.ShouldCopy)
                {
                    continue;
                }

                if (!planned.IsPriority)
                {
                    var waitTask = _runtimeGate.WaitUntilNoPriorityPendingAsync(cancellationToken);
                    var hasWaited = !waitTask.IsCompleted;
                    if (hasWaited)
                    {
                        _runtimeGate.UpdateState(
                            job,
                            BackupStatus.Waiting,
                            totalFiles,
                            totalSize,
                            remainingFiles,
                            remainingSize,
                            totalFiles > 0 ? (int)(100.0 * (totalFiles - remainingFiles) / totalFiles) : 0,
                            planned.SourceFile,
                            planned.DestinationFile);
                    }

                    _runtimeGate.WaitUntilPriorityBarrierAllowsCopy(
                        waitTask,
                        job,
                        totalFiles,
                        totalSize,
                        remainingFiles,
                        remainingSize,
                        planned.SourceFile,
                        planned.DestinationFile,
                        cancellationToken);

                    if (hasWaited)
                    {
                        _runtimeGate.UpdateState(
                            job,
                            BackupStatus.Active,
                            totalFiles,
                            totalSize,
                            remainingFiles,
                            remainingSize,
                            totalFiles > 0 ? (int)(100.0 * (totalFiles - remainingFiles) / totalFiles) : 0,
                            planned.SourceFile,
                            planned.DestinationFile);
                    }
                }
                _runtimeGate.WaitUntilBusinessSoftwareAllowsCopy(
                        job,
                        planned.SourceFile,
                        planned.DestinationFile,
                        totalFiles,
                        totalSize,
                        remainingFiles,
                        remainingSize,
                        cancellationToken);

                var destinationPreparation = _fileExecutionService.PrepareDestinationDirectory(planned.DestinationFile);
                if (destinationPreparation.Created)
                {
                    _runtimeGate.Log(
                        job.Id,
                        job.Name,
                        LogEventType.CreateDirectory,
                        destinationPreparation.DirectoryPath,
                        destinationPreparation.DirectoryPath,
                        0,
                        0
                    );
                }

                BackupFileTransferExecutionResult fileExecutionResult;
                using (_runtimeGate.WaitUntilLargeFileBarrierAllowsCopy(
                           job,
                           totalFiles,
                           totalSize,
                           remainingFiles,
                           remainingSize,
                           planned.SourceFile,
                           planned.DestinationFile,
                           planned.SourceFileSizeBytes,
                           parallelLargeFileThresholdBytes,
                           cancellationToken))
                {
                    fileExecutionResult = _fileExecutionService.TransferAndEncrypt(planned, encryptionPolicy);
                }

                _runtimeGate.Log(job.Id,
                    job.Name,
                    LogEventType.TransferFile,
                    planned.SourceFile,
                    planned.DestinationFile,
                    fileExecutionResult.TransferResult.FileSizeBytes,
                    fileExecutionResult.TransferResult.TransferTimeMs,
                    fileExecutionResult.EncryptionTimeMs
                );

                if (planned.IsPriority)
                {
                    _runtimeGate.MarkPriorityFileCompleted(job.Id);
                }

                remainingFiles--;
                remainingSize -= fileExecutionResult.TransferResult.FileSizeBytes;

                int progress = totalFiles > 0
                    ? (int)(100.0 * (totalFiles - remainingFiles) / totalFiles)
                    : 0;

                _runtimeGate.UpdateState(job, BackupStatus.Active, totalFiles, totalSize, remainingFiles, remainingSize, progress, planned.SourceFile, planned.DestinationFile);
                _runtimeGate.LogPendingUserActions(job);
                _runtimeGate.WaitForRuntimeControlAndSyncState(
                    job,
                    totalFiles,
                    totalSize,
                    remainingFiles,
                    remainingSize,
                    planned.SourceFile,
                    planned.DestinationFile);
            }
            backupLoopTimer.Stop();
            totalDurationMs = backupLoopTimer.ElapsedMilliseconds;

            _runtimeGate.LogPendingUserActions(job);
            _runtimeGate.WaitForRuntimeControlAndSyncState(
                job,
                totalFiles,
                totalSize,
                remainingFiles,
                remainingSize,
                string.Empty,
                string.Empty);
            _runtimeGate.UpdateState(job, BackupStatus.Done, totalFiles, totalSize, 0, 0, 100, "", "");
            _runtimeGate.Log(job.Id, job.Name, LogEventType.EndBackup, "", "", totalSize, totalDurationMs);
            _stateWriter.MarkInactive(job.Id);
        }
        catch (Exception ex)
        {
            if (backupLoopTimer is not null)
            {
                totalDurationMs = backupLoopTimer.ElapsedMilliseconds;
            }

            bool stoppedByUser = IsStoppedByUser(ex);
            bool blockedByBusinessSoftware = _runtimeGate.IsBusinessSoftwareBlocked(ex);
            string sourceContext = ex.Data["errorKey"]?.ToString() ?? ex.GetType().Name;
            string destinationContext = ex.Data["0"]?.ToString() ?? ex.Message;

            if (stoppedByUser)
            {
                sourceContext = ex.Data["actionKey"]?.ToString() ?? BackupRuntimeKeys.ActionBackupStoppedByUser;
                destinationContext = string.Empty;
                _stateWriter.MarkInactive(job.Id);
            }
            else
            {
                _runtimeGate.UpdateState(job, BackupStatus.Error, 0, 0, 0, 0, 0, sourceContext, destinationContext);
            }

            var eventType = stoppedByUser
                ? LogEventType.Stopped
                : blockedByBusinessSoftware
                ? LogEventType.BusinessSoftwareDetected
                : LogEventType.Error;
            _runtimeGate.Log(
                job.Id,
                job.Name,
                eventType,
                sourceContext,
                destinationContext,
                0,
                totalDurationMs
            );
            throw;
        }
        finally
        {
            _runtimeGate.EndJob(job.Id);

            if (barrierRegistered)
            {
                _runtimeGate.UnregisterPriorityJob(job.Id);
            }
        }
    }
    #endregion

    private static bool IsStoppedByUser(Exception ex)
    {
        return string.Equals(ex.Data["errorKey"]?.ToString(), BackupRuntimeKeys.ErrorBackupStoppedByUser, StringComparison.Ordinal)
            || string.Equals(ex.Data["actionKey"]?.ToString(), BackupRuntimeKeys.ActionBackupStoppedByUser, StringComparison.Ordinal)
            || string.Equals(ex.Message, BackupRuntimeKeys.ErrorBackupStoppedByUser, StringComparison.Ordinal);
    }

}
