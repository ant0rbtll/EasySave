using EasySave.Core;
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

    public void BeginJob(int jobId) => _executionController.BeginJob(jobId);

    public void EndJob(int jobId) => _executionController.EndJob(jobId);

    public void RegisterPriorityJob(int jobId, int pendingPriorityFiles) => _priorityFilesBarrier.RegisterJob(jobId, pendingPriorityFiles);

    public void MarkPriorityFileCompleted(int jobId) => _priorityFilesBarrier.MarkPriorityFileCompleted(jobId);

    public void UnregisterPriorityJob(int jobId) => _priorityFilesBarrier.UnregisterJob(jobId);

    public void LogPendingUserActions(BackupJob job) => _reporter.LogPendingUserActions(job, _executionController);

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

    public Task WaitUntilNoPriorityPendingAsync(CancellationToken cancellationToken = default)
        => _priorityFilesBarrier.WaitUntilNoPriorityPendingAsync(cancellationToken);

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
                var blockedProcess = ex.Data["0"]?.ToString() ?? string.Empty;

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

    public bool IsBusinessSoftwareBlocked(Exception ex)
    {
        return string.Equals(ex.Data["errorKey"]?.ToString(), BusinessSoftwareErrorKey, StringComparison.Ordinal);
    }

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
