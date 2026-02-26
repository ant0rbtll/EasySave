using EasySave.Core;
using EasySave.Localization;
using EasySave.Log;
using EasySave.System;
using EasySave.State;

namespace EasySave.Backup;

/// <summary>
/// Encapsulates runtime control and cross-job gating during backup execution.
/// </summary>
public sealed class BackupRuntimeGate(
    IBackupExecutionController executionController,
    IPriorityFilesBarrier priorityFilesBarrier,
    ILargeFileTransferBarrier largeFileTransferBarrier,
    IBackupExecutionGuard executionGuard,
    BackupExecutionReporter reporter)
{
    private readonly IBackupExecutionController _executionController = executionController;
    private readonly IPriorityFilesBarrier _priorityFilesBarrier = priorityFilesBarrier;
    private readonly ILargeFileTransferBarrier _largeFileTransferBarrier = largeFileTransferBarrier;
    private readonly IBackupExecutionGuard _executionGuard = executionGuard;
    private readonly BackupExecutionReporter _reporter = reporter;

    private const string BusinessSoftwareErrorKey = BackupRuntimeKeys.ErrorBusinessSoftwareRunning;
    private const int BusinessSoftwareRetryDelayMs = 500;
    private const int PriorityBarrierPollDelayMs = 100;
    private const int LargeFileBarrierPollDelayMs = 100;

    /// <summary>
    /// Initializes runtime state for a running job.
    /// </summary>
    /// <param name="jobId">Backup job identifier.</param>
    public void BeginJob(int jobId) => _executionController.BeginJob(jobId);

    /// <summary>
    /// Clears runtime state for a completed/stopped job.
    /// </summary>
    /// <param name="jobId">Backup job identifier.</param>
    public void EndJob(int jobId) => _executionController.EndJob(jobId);

    /// <summary>
    /// Registers the number of pending priority files for one running job.
    /// </summary>
    /// <param name="jobId">Backup job identifier.</param>
    /// <param name="pendingPriorityFiles">Pending priority file count.</param>
    public void RegisterPriorityJob(int jobId, int pendingPriorityFiles) => _priorityFilesBarrier.RegisterJob(jobId, pendingPriorityFiles);

    /// <summary>
    /// Marks one priority file as processed for a job.
    /// </summary>
    /// <param name="jobId">Backup job identifier.</param>
    public void MarkPriorityFileCompleted(int jobId) => _priorityFilesBarrier.MarkPriorityFileCompleted(jobId);

    /// <summary>
    /// Unregisters a job from the priority-file barrier.
    /// </summary>
    /// <param name="jobId">Backup job identifier.</param>
    public void UnregisterPriorityJob(int jobId) => _priorityFilesBarrier.UnregisterJob(jobId);

    /// <summary>
    /// Logs pending user actions emitted by runtime controls (pause/stop).
    /// </summary>
    /// <param name="job">Backup job context.</param>
    public void LogPendingUserActions(BackupJob job) => _reporter.LogPendingUserActions(job, _executionController);

    /// <summary>
    /// Forwards a state update to the execution reporter.
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
        => _reporter.UpdateState(
            job,
            status,
            totalFiles,
            totalSize,
            remainingFiles,
            remainingSize,
            progress,
            sourcePath,
            destinationPath);

    /// <summary>
    /// Forwards one log entry to the execution reporter.
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
        => _reporter.Log(
            backupId,
            backupName,
            eventType,
            sourcePath,
            destinationPath,
            fileSizeBytes,
            transferTimeMs,
            encryptionTimeMs);

    /// <summary>
    /// Waits asynchronously until no priority files remain pending across running jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task WaitUntilNoPriorityPendingAsync(CancellationToken cancellationToken = default)
        => _priorityFilesBarrier.WaitUntilNoPriorityPendingAsync(cancellationToken);

    /// <summary>
    /// Waits for priority-file barrier release while keeping pause/stop responsive and state synchronized.
    /// </summary>
    /// <param name="waitTask">Barrier wait task.</param>
    /// <param name="job">Backup job context.</param>
    /// <param name="totalFiles">Total file count for this run.</param>
    /// <param name="totalSize">Total size in bytes for this run.</param>
    /// <param name="remainingFiles">Remaining file count.</param>
    /// <param name="remainingSize">Remaining size in bytes.</param>
    /// <param name="sourceFile">Current source file.</param>
    /// <param name="destinationFile">Current destination file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public void WaitUntilPriorityBarrierAllowsCopy(
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
                _reporter.UpdateState(
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
    /// Acquires a large-file transfer lease when required while keeping pause/stop responsive.
    /// </summary>
    /// <param name="job">Backup job context.</param>
    /// <param name="totalFiles">Total file count for this run.</param>
    /// <param name="totalSize">Total size in bytes for this run.</param>
    /// <param name="remainingFiles">Remaining file count.</param>
    /// <param name="remainingSize">Remaining size in bytes.</param>
    /// <param name="sourceFile">Current source file.</param>
    /// <param name="destinationFile">Current destination file.</param>
    /// <param name="sourceFileSizeBytes">Current source file size in bytes.</param>
    /// <param name="thresholdBytes">Large-file threshold in bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A lease to dispose when a slot was acquired; otherwise <see langword="null"/>.</returns>
    public IDisposable? WaitUntilLargeFileBarrierAllowsCopy(
        BackupJob job,
        int totalFiles,
        long totalSize,
        int remainingFiles,
        long remainingSize,
        string sourceFile,
        string destinationFile,
        long sourceFileSizeBytes,
        long thresholdBytes,
        CancellationToken cancellationToken)
    {
        var waitingStateDisplayed = false;
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

            var acquireResult = _largeFileTransferBarrier.TryAcquire(
                sourceFileSizeBytes,
                thresholdBytes,
                out var lease);

            if (acquireResult is LargeFileTransferAcquireResult.NotRequired or LargeFileTransferAcquireResult.Acquired)
            {
                if (waitingStateDisplayed)
                {
                    _reporter.UpdateState(
                        job,
                        BackupStatus.Active,
                        totalFiles,
                        totalSize,
                        remainingFiles,
                        remainingSize,
                        totalFiles > 0 ? (int)(100.0 * (totalFiles - remainingFiles) / totalFiles) : 0,
                        sourceFile,
                        destinationFile);
                }

                return lease;
            }

            if (!waitingStateDisplayed || resumedFromPause)
            {
                waitingStateDisplayed = true;
                _reporter.UpdateState(
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

            Task.Delay(LargeFileBarrierPollDelayMs, cancellationToken).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Blocks execution while configured business software is running, with live blocked state updates.
    /// </summary>
    /// <param name="job">Backup job context.</param>
    /// <param name="sourceFile">Current source file.</param>
    /// <param name="destinationFile">Current destination file.</param>
    /// <param name="totalFiles">Total file count for this run.</param>
    /// <param name="totalSize">Total size in bytes for this run.</param>
    /// <param name="remainingFiles">Remaining file count.</param>
    /// <param name="remainingSize">Remaining size in bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public void WaitUntilBusinessSoftwareAllowsCopy(
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
                var blockedProcess = GetBusinessSoftwareProcessName(ex);

                _reporter.UpdateState(
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
                    _reporter.Log(
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

    /// <summary>
    /// Determines whether an exception indicates business software blocking.
    /// </summary>
    /// <param name="ex">Exception to inspect.</param>
    /// <returns><see langword="true"/> when the exception carries the business-software error key.</returns>
    public bool IsBusinessSoftwareBlocked(Exception ex)
    {
        return string.Equals(ex.Data["errorKey"]?.ToString(), BusinessSoftwareErrorKey, StringComparison.Ordinal)
            || ex is ITranslatableException translatable
            && translatable.ErrorKey == LocalizationKey.error_business_software_running;
    }

    private static string GetBusinessSoftwareProcessName(Exception ex)
    {
        var fromData = ex.Data["0"]?.ToString();
        if (!string.IsNullOrWhiteSpace(fromData))
        {
            return fromData;
        }

        if (ex is ITranslatableException translatable
            && translatable.ErrorKey == LocalizationKey.error_business_software_running
            && translatable.Options.Count > 0)
        {
            return translatable.Options[0];
        }

        return string.Empty;
    }

    /// <summary>
    /// Applies runtime pause/stop control and keeps priority barrier + state synchronized.
    /// </summary>
    /// <param name="job">Backup job context.</param>
    /// <param name="totalFiles">Total file count for this run.</param>
    /// <param name="totalSize">Total size in bytes for this run.</param>
    /// <param name="remainingFiles">Remaining file count.</param>
    /// <param name="remainingSize">Remaining size in bytes.</param>
    /// <param name="sourcePath">Current source path context.</param>
    /// <param name="destinationPath">Current destination path context.</param>
    /// <returns><see langword="true"/> when execution resumed from paused state.</returns>
    public bool WaitForRuntimeControlAndSyncState(
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
            _reporter.UpdateState(
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
            _reporter.UpdateState(
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
