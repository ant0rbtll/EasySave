using EasySave.Core;
using EasySave.Log;
using EasySave.System;
using EasySave.State;
using EasySave.Persistence;
using System.Diagnostics;
using System.Threading;

namespace EasySave.Backup;

/// <summary>
/// Executes complete and differential backups.
/// </summary>
/// <remarks>
/// Initializes a new instance of the backup engine.
/// </remarks>
/// <param name="fileSystem">File system management service.</param>
/// <param name="transferService">File transfer service.</param>
/// <param name="stateWriter">Backup state writer service.</param>
/// <param name="logger">Logging service.</param>
public class BackupEngine(
    IFileSystem fileSystem,
    ITransferService transferService,
    IStateWriter stateWriter,
    ILogger logger,
    IUserPreferencesRepository? preferencesRepository = null,
    IEncryptionPolicyProvider? encryptionPolicyProvider = null,
    IEncryptionProviderResolver? encryptionProviderResolver = null,
    IBackupExecutionGuard? executionGuard = null,
    IPriorityFilesBarrier? priorityFilesBarrier = null,
    IBackupFilePlanner? filePlanner = null,
    IBackupExecutionController? executionController = null,
    ILargeFileTransferBarrier? largeFileTransferBarrier = null) : IBackupEngine
{
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ITransferService _transferService = transferService;
    private readonly IStateWriter _stateWriter = stateWriter;
    private readonly ILogger _logger = logger;
    private readonly IUserPreferencesRepository? _preferencesRepository = preferencesRepository;
    private readonly IEncryptionPolicyProvider _encryptionPolicyProvider = encryptionPolicyProvider ?? new NoOpEncryptionPolicyProvider();
    private readonly IEncryptionProviderResolver _encryptionProviderResolver = encryptionProviderResolver ?? new NoOpEncryptionProviderResolver();
    private readonly IBackupExecutionGuard _executionGuard = executionGuard ?? new NoOpBackupExecutionGuard();
    private readonly IPriorityFilesBarrier _priorityFilesBarrier = priorityFilesBarrier ?? new NoOpPriorityFilesBarrier();
    private readonly IBackupFilePlanner _filePlanner = filePlanner ?? new DefaultBackupFilePlanner(fileSystem);
    private readonly IBackupExecutionController _executionController = executionController ?? new NoOpBackupExecutionController();
    private readonly ILargeFileTransferBarrier _largeFileTransferBarrier = largeFileTransferBarrier ?? new NoOpLargeFileTransferBarrier();
    private const string BusinessSoftwareErrorKey = BackupRuntimeKeys.ErrorBusinessSoftwareRunning;
    private const int BusinessSoftwareRetryDelayMs = 500;
    private const int PriorityBarrierPollDelayMs = 100;

    /// <summary>
    /// Executes a complete or differential backup.
    /// </summary>
    /// <param name="job">Backup job to execute.</param>
    /// <exception cref="NotSupportedException">The backup type is not supported.</exception>
    /// <inheritdoc />
    public Task Execute(BackupJob job, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ExecuteCore(job, cancellationToken), cancellationToken);
    }

    private void ExecuteCore(BackupJob job, CancellationToken cancellationToken)
    {
        long totalDurationMs = 0;
        Stopwatch? backupLoopTimer = null;
        _executionController.BeginJob(job.Id);
        bool barrierRegistered = false;
        try
        {
            var preferences = _preferencesRepository?.Load();
            var plannedFiles = _filePlanner.BuildPlans(job, preferences?.PriorityExtensions);
            var parallelLargeFileThresholdBytes = preferences?.GetParallelLargeFileThresholdBytes() ?? 0;
            var encryptionPolicy = _encryptionPolicyProvider.GetPolicy() ?? EncryptionPolicy.Disabled;

            int totalFiles = plannedFiles.Count;
            long totalSize = plannedFiles.Sum(f => _fileSystem.GetFileSize(f.SourceFile));

            int remainingFiles = totalFiles;
            long remainingSize = totalSize;

            UpdateState(job, BackupStatus.Active, totalFiles, totalSize, remainingFiles, remainingSize, 0, "", "");
            Log(job.Id, job.Name, LogEventType.StartBackup, "", "", totalSize, 0);

            _priorityFilesBarrier.RegisterJob(job.Id, plannedFiles.Count(f => f.ShouldCopy && f.IsPriority));
            barrierRegistered = true;

            backupLoopTimer = Stopwatch.StartNew();
            foreach (var planned in plannedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LogPendingUserActions(job);

                WaitForRuntimeControlAndSyncState(
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
                    var waitTask = _priorityFilesBarrier.WaitUntilNoPriorityPendingAsync(cancellationToken);
                    var hasWaited = !waitTask.IsCompleted;
                    if (hasWaited)
                    {
                        UpdateState(
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

                    WaitUntilPriorityBarrierAllowsCopy(
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
                        UpdateState(
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
                WaitUntilBusinessSoftwareAllowsCopy(
                        job,
                        planned.SourceFile,
                        planned.DestinationFile,
                        totalFiles,
                        totalSize,
                        remainingFiles,
                        remainingSize,
                        cancellationToken);

                var destinationDir = Path.GetDirectoryName(planned.DestinationFile)!;

                if (!_fileSystem.DirectoryExists(destinationDir))
                {
                    _fileSystem.CreateDirectory(destinationDir);
                    Log(
                        job.Id,
                        job.Name,
                        LogEventType.CreateDirectory,
                        destinationDir,
                        destinationDir,
                        0,
                        0
                    );
                }

                    // Check pause/stop one more time right before transfer.
                    // This narrows the race window where pause could be requested
                    // after the pre-loop check but before starting the next file copy.
                    WaitForRuntimeControlAndSyncState(
                        job,
                        totalFiles,
                        totalSize,
                        remainingFiles,
                        remainingSize,
                        planned.SourceFile,
                        planned.DestinationFile);

                var sourceFileSize = _fileSystem.GetFileSize(planned.SourceFile);
                var waitedForLargeFileSlot = false;
                using var largeFileTransferLease = _largeFileTransferBarrier.Acquire(
                    sourceFileSize,
                    parallelLargeFileThresholdBytes,
                    cancellationToken,
                    onWaitingForSlot: () =>
                    {
                        waitedForLargeFileSlot = true;
                        UpdateState(
                            job,
                            BackupStatus.Waiting,
                            totalFiles,
                            totalSize,
                            remainingFiles,
                            remainingSize,
                            totalFiles > 0 ? (int)(100.0 * (totalFiles - remainingFiles) / totalFiles) : 0,
                            planned.SourceFile,
                            planned.DestinationFile);
                    });
                if (waitedForLargeFileSlot)
                {
                    UpdateState(
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
                TransferResult result = _transferService.TransferFile(planned.SourceFile, planned.DestinationFile, true);

                if (!result.IsSuccess)
                {
                    var message = $"File transfer failed from {planned.SourceFile} to {planned.DestinationFile} with error code {result.ErrorCode}";
                    var e = new InvalidOperationException(message);
                    e.Data["errorKey"] = "error_file_transfer_failed";
                    e.Data["0_from"] = planned.SourceFile;
                    e.Data["1_destination"] = planned.DestinationFile;
                    e.Data["2_errorCode"] = result.ErrorCode;
                    throw e;
                }

                long encryptionTimeMs = EncryptTransferredFileIfRequired(planned.DestinationFile, encryptionPolicy);

                Log(job.Id,
                    job.Name,
                    LogEventType.TransferFile,
                    planned.SourceFile,
                    planned.DestinationFile,
                    result.FileSizeBytes,
                    result.TransferTimeMs,
                    encryptionTimeMs
                );

                if (planned.IsPriority)
                {
                    _priorityFilesBarrier.MarkPriorityFileCompleted(job.Id);
                }

                remainingFiles--;
                remainingSize -= result.FileSizeBytes;

                int progress = totalFiles > 0
                    ? (int)(100.0 * (totalFiles - remainingFiles) / totalFiles)
                    : 0;

                UpdateState(job, BackupStatus.Active, totalFiles, totalSize, remainingFiles, remainingSize, progress, planned.SourceFile, planned.DestinationFile);
                    LogPendingUserActions(job);
                    WaitForRuntimeControlAndSyncState(
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

            LogPendingUserActions(job);
            WaitForRuntimeControlAndSyncState(
                job,
                totalFiles,
                totalSize,
                remainingFiles,
                remainingSize,
                string.Empty,
                string.Empty);
            UpdateState(job, BackupStatus.Done, totalFiles, totalSize, 0, 0, 100, "", "");
            Log(job.Id, job.Name, LogEventType.EndBackup, "", "", totalSize, totalDurationMs);
            _stateWriter.MarkInactive(job.Id);
        }
        catch (Exception ex)
        {
            if (backupLoopTimer is not null)
            {
                totalDurationMs = backupLoopTimer.ElapsedMilliseconds;
            }

            bool stoppedByUser = IsStoppedByUser(ex);
            bool blockedByBusinessSoftware = IsBusinessSoftwareBlocked(ex);
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
                UpdateState(job, BackupStatus.Error, 0, 0, 0, 0, 0, sourceContext, destinationContext);
            }

            var eventType = stoppedByUser
                ? LogEventType.Stopped
                : blockedByBusinessSoftware
                ? LogEventType.BusinessSoftwareDetected
                : LogEventType.Error;
            Log(
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
            _executionController.EndJob(job.Id);

            if (barrierRegistered)
            {
                _priorityFilesBarrier.UnregisterJob(job.Id);
            }
        }
    }

    private void WaitUntilPriorityBarrierAllowsCopy(
        Task waitTask,
        BackupJob job,
        int totalFiles,
        long totalSize,
        int remainingFiles,
        long remainingSize,
        string sourceFile,
        string destinationFile,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LogPendingUserActions(job);
            var resumedFromPause = WaitForRuntimeControlAndSyncState(
                job,
                totalFiles,
                totalSize,
                remainingFiles,
                remainingSize,
                sourceFile,
                destinationFile);

            if (waitTask.IsCompleted)
            {
                waitTask.GetAwaiter().GetResult();
                return;
            }

            if (resumedFromPause)
            {
                UpdateState(
                    job,
                    BackupStatus.Waiting,
                    totalFiles,
                    totalSize,
                    remainingFiles,
                    remainingSize,
                    totalFiles > 0 ? (int)(100.0 * (totalFiles - remainingFiles) / totalFiles) : 0,
                    sourceFile,
                    destinationFile);
            }

            Task.Delay(PriorityBarrierPollDelayMs, cancellationToken).GetAwaiter().GetResult();
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
    /// <param name="encryptionTimeMs">Encryption time in milliseconds.</param>
    private void Log(
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
            )
            );
        }
        catch (Exception)
        {
            // Logging failures must not interrupt backup execution.
        }
    }

    private void LogPendingUserActions(BackupJob job)
    {
        while (_executionController.TryDequeueAction(out var actionKey))
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

    private void WaitUntilBusinessSoftwareAllowsCopy(
        BackupJob job,
        string sourceFile,
        string destinationFile,
        int totalFiles,
        long totalSize,
        int remainingFiles,
        long remainingSize,
        CancellationToken cancellationToken)
    {
        var blockedLogged = false;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LogPendingUserActions(job);
            WaitForRuntimeControlAndSyncState(
                job,
                totalFiles,
                totalSize,
                remainingFiles,
                remainingSize,
                sourceFile,
                destinationFile);

            try
            {
                _executionGuard.EnsureCanCopyNextFile();
                return;
            }
            catch (Exception ex) when (IsBusinessSoftwareBlocked(ex))
            {
                var blockedProcess = ex.Data["0"]?.ToString() ?? string.Empty;

                // Keep the backup active and expose the temporary block in live state.
                UpdateState(
                    job,
                    BackupStatus.Blocked,
                    totalFiles,
                    totalSize,
                    remainingFiles,
                    remainingSize,
                    totalFiles > 0 ? (int)(100.0 * (totalFiles - remainingFiles) / totalFiles) : 0,
                    BusinessSoftwareErrorKey,
                    blockedProcess);

                if (!blockedLogged)
                {
                    blockedLogged = true;
                    Log(
                        job.Id,
                        job.Name,
                        LogEventType.BusinessSoftwareDetected,
                        BusinessSoftwareErrorKey,
                        blockedProcess,
                        0,
                        0);
                }

                Task.Delay(BusinessSoftwareRetryDelayMs, cancellationToken).GetAwaiter().GetResult();
            }
        }
    }

    private static bool IsBusinessSoftwareBlocked(Exception ex)
    {
        return string.Equals(ex.Data["errorKey"]?.ToString(), BusinessSoftwareErrorKey, StringComparison.Ordinal);
    }

    private static bool IsStoppedByUser(Exception ex)
    {
        return string.Equals(ex.Data["errorKey"]?.ToString(), BackupRuntimeKeys.ErrorBackupStoppedByUser, StringComparison.Ordinal)
            || string.Equals(ex.Data["actionKey"]?.ToString(), BackupRuntimeKeys.ActionBackupStoppedByUser, StringComparison.Ordinal)
            || string.Equals(ex.Message, BackupRuntimeKeys.ErrorBackupStoppedByUser, StringComparison.Ordinal);
    }

    private long EncryptTransferredFileIfRequired(string destinationFile, EncryptionPolicy policy)
    {
        if (!policy.ShouldEncrypt(destinationFile))
        {
            return 0;
        }

        var provider = _encryptionProviderResolver.Resolve(policy.ProviderName);
        if (provider is null)
        {
            return -1;
        }

        try
        {
            // Encryption is intentionally serialized/synchronous due to product constraints on the provider.
            var result = provider.EncryptAsync(destinationFile, policy).GetAwaiter().GetResult();
            if (result.IsSuccess)
            {
                return Math.Max(0, result.EncryptionTimeMs);
            }

            return result.EncryptionTimeMs < 0
                ? result.EncryptionTimeMs
                : -Math.Max(1, result.EncryptionTimeMs);
        }
        catch (Exception)
        {
            // Encryption failures must not interrupt backup execution.
            return -1;
        }
    }

    private bool WaitForRuntimeControlAndSyncState(
        BackupJob job,
        int totalFiles,
        long totalSize,
        int remainingFiles,
        long remainingSize,
        string sourcePath,
        string destinationPath)
    {
        var progress = totalFiles > 0
            ? (int)(100.0 * (totalFiles - remainingFiles) / totalFiles)
            : 0;

        var hasControlState = _executionController.TryGetCurrentJobControlState(job.Id, out var controlState);
        var isPaused = hasControlState && controlState == BackupJobControlState.Paused;

        if (isPaused)
        {
            _priorityFilesBarrier.PauseJob(job.Id);
            UpdateState(
                job,
                BackupStatus.Paused,
                totalFiles,
                totalSize,
                remainingFiles,
                remainingSize,
                progress,
                sourcePath,
                destinationPath);
        }

        _executionController.WaitIfPausedOrThrowIfStopped();

        if (isPaused)
        {
            _priorityFilesBarrier.ResumeJob(job.Id);
            UpdateState(
                job,
                BackupStatus.Active,
                totalFiles,
                totalSize,
                remainingFiles,
                remainingSize,
                progress,
                sourcePath,
                destinationPath);
        }

        return isPaused;
    }

}
