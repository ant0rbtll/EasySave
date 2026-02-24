using EasySave.Core;
using EasySave.Log;
using EasySave.State;
using EasySave.System;
using Moq;

namespace EasySave.Backup.Tests;

public class BackupRuntimeGateTests
{
    private readonly BackupJob _job = new()
    {
        Id = 1,
        Name = "job",
        Source = "/src",
        Destination = "/dst",
        Type = BackupType.Complete
    };

    [Fact]
    public void BeginEndAndPriorityMethods_AreForwardedToCollaborators()
    {
        var executionControllerMock = new Mock<IBackupExecutionController>();
        var priorityBarrierMock = new Mock<IPriorityFilesBarrier>();
        var gate = CreateGate(
            executionController: executionControllerMock.Object,
            priorityBarrier: priorityBarrierMock.Object);

        gate.BeginJob(7);
        gate.RegisterPriorityJob(7, 3);
        gate.MarkPriorityFileCompleted(7);
        gate.UnregisterPriorityJob(7);
        gate.EndJob(7);

        executionControllerMock.Verify(c => c.BeginJob(7), Times.Once);
        executionControllerMock.Verify(c => c.EndJob(7), Times.Once);
        priorityBarrierMock.Verify(b => b.RegisterJob(7, 3), Times.Once);
        priorityBarrierMock.Verify(b => b.MarkPriorityFileCompleted(7), Times.Once);
        priorityBarrierMock.Verify(b => b.UnregisterJob(7), Times.Once);
    }

    [Fact]
    public async Task WaitUntilNoPriorityPendingAsync_IsForwarded()
    {
        var priorityBarrierMock = new Mock<IPriorityFilesBarrier>();
        priorityBarrierMock
            .Setup(b => b.WaitUntilNoPriorityPendingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var gate = CreateGate(priorityBarrier: priorityBarrierMock.Object);

        await gate.WaitUntilNoPriorityPendingAsync();

        priorityBarrierMock.Verify(b => b.WaitUntilNoPriorityPendingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void LogPendingUserActions_WritesActionLogs()
    {
        var loggerMock = new Mock<ILogger>();
        var stateWriterMock = new Mock<IStateWriter>();
        var controller = new ActionQueueExecutionController(["action_pause", "action_resume"]);
        var gate = CreateGate(stateWriter: stateWriterMock.Object, logger: loggerMock.Object, executionController: controller);

        gate.LogPendingUserActions(_job);

        loggerMock.Verify(l => l.Write(It.Is<LogEntry>(e =>
            e.EventType == LogEventType.Action && e.SourcePathUNC == "action_pause")), Times.Once);
        loggerMock.Verify(l => l.Write(It.Is<LogEntry>(e =>
            e.EventType == LogEventType.Action && e.SourcePathUNC == "action_resume")), Times.Once);
    }

    [Fact]
    public void UpdateState_AndLog_AreForwardedToReporter()
    {
        var loggerMock = new Mock<ILogger>();
        var stateWriterMock = new Mock<IStateWriter>();
        var gate = CreateGate(stateWriter: stateWriterMock.Object, logger: loggerMock.Object);

        gate.UpdateState(_job, BackupStatus.Active, 2, 20, 1, 10, 50, "/src/a.txt", "/dst/a.txt");
        gate.Log(_job.Id, _job.Name, LogEventType.StartBackup, "s", "d", 1, 2, 3);

        stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(entry =>
            entry.BackupId == _job.Id &&
            entry.Status == BackupStatus.Active &&
            entry.ProgressPercent == 50)), Times.Once);
        loggerMock.Verify(l => l.Write(It.Is<LogEntry>(entry =>
            entry.BackupId == _job.Id &&
            entry.EventType == LogEventType.StartBackup &&
            entry.EncryptionTimeMs == 3)), Times.Once);
    }

    [Fact]
    public void WaitForRuntimeControlAndSyncState_WhenPaused_UpdatesStateAndPausesPriorityBarrier()
    {
        var stateWriterMock = new Mock<IStateWriter>();
        var priorityBarrierMock = new Mock<IPriorityFilesBarrier>();
        var executionController = new Mock<IBackupExecutionController>();
        executionController
            .Setup(c => c.TryGetCurrentJobControlState(_job.Id, out It.Ref<BackupJobControlState>.IsAny))
            .Returns((int _, out BackupJobControlState state) =>
            {
                state = BackupJobControlState.Paused;
                return true;
            });

        var gate = CreateGate(
            stateWriter: stateWriterMock.Object,
            priorityBarrier: priorityBarrierMock.Object,
            executionController: executionController.Object);

        var resumed = gate.WaitForRuntimeControlAndSyncState(_job, 10, 100, 8, 80, "/src/a.txt", "/dst/a.txt");

        Assert.True(resumed);
        priorityBarrierMock.Verify(b => b.PauseJob(_job.Id), Times.Once);
        priorityBarrierMock.Verify(b => b.ResumeJob(_job.Id), Times.Once);
        stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Paused)), Times.Once);
        stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Active)), Times.Once);
    }

    [Fact]
    public void WaitUntilPriorityBarrierAllowsCopy_WhenTaskFaulted_PropagatesException()
    {
        var gate = CreateGate();
        var expected = new InvalidOperationException("priority failed");
        var faultedWaitTask = Task.FromException(expected);

        var ex = Assert.Throws<InvalidOperationException>(() => gate.WaitUntilPriorityBarrierAllowsCopy(
            faultedWaitTask,
            _job,
            1,
            10,
            1,
            10,
            "/src/a.txt",
            "/dst/a.txt",
            CancellationToken.None));

        Assert.Equal("priority failed", ex.Message);
    }

    [Fact]
    public void WaitUntilPriorityBarrierAllowsCopy_WhenResumedWhileWaiting_UpdatesWaitingState()
    {
        var stateWriterMock = new Mock<IStateWriter>();
        var priorityBarrierMock = new Mock<IPriorityFilesBarrier>();
        var executionController = new Mock<IBackupExecutionController>();
        executionController
            .Setup(c => c.TryGetCurrentJobControlState(_job.Id, out It.Ref<BackupJobControlState>.IsAny))
            .Returns((int _, out BackupJobControlState state) =>
            {
                state = BackupJobControlState.Paused;
                return true;
            });
        var gate = CreateGate(
            stateWriter: stateWriterMock.Object,
            priorityBarrier: priorityBarrierMock.Object,
            executionController: executionController.Object);

        var tcs = new TaskCompletionSource<bool>();
        _ = Task.Run(async () =>
        {
            await Task.Delay(40);
            tcs.TrySetResult(true);
        });

        gate.WaitUntilPriorityBarrierAllowsCopy(
            tcs.Task,
            _job,
            4,
            40,
            3,
            30,
            "/src/a.txt",
            "/dst/a.txt",
            CancellationToken.None);

        stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Waiting)), Times.AtLeastOnce);
    }

    [Fact]
    public void WaitUntilLargeFileBarrierAllowsCopy_WhenBusyThenAcquired_TransitionsWaitingToActive()
    {
        var stateWriterMock = new Mock<IStateWriter>();
        var gate = CreateGate(
            stateWriter: stateWriterMock.Object,
            largeBarrier: new SequenceLargeFileTransferBarrier(
                LargeFileTransferAcquireResult.Busy,
                LargeFileTransferAcquireResult.Acquired));

        using var lease = gate.WaitUntilLargeFileBarrierAllowsCopy(
            _job,
            5,
            50,
            4,
            40,
            "/src/large.bin",
            "/dst/large.bin",
            2000,
            1000,
            CancellationToken.None);

        Assert.NotNull(lease);
        stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Waiting)), Times.AtLeastOnce);
        stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Active)), Times.AtLeastOnce);
    }

    [Fact]
    public void WaitUntilLargeFileBarrierAllowsCopy_WhenNotRequired_ReturnsNullLease()
    {
        var gate = CreateGate(
            largeBarrier: new SequenceLargeFileTransferBarrier(LargeFileTransferAcquireResult.NotRequired));

        var lease = gate.WaitUntilLargeFileBarrierAllowsCopy(
            _job,
            1,
            10,
            1,
            10,
            "/src/small.bin",
            "/dst/small.bin",
            100,
            1000,
            CancellationToken.None);

        Assert.Null(lease);
    }

    [Fact]
    public void WaitUntilBusinessSoftwareAllowsCopy_WhenBlockedThenUnblocked_LogsAndReturns()
    {
        var stateWriterMock = new Mock<IStateWriter>();
        var loggerMock = new Mock<ILogger>();
        var guardMock = new Mock<IBackupExecutionGuard>();
        var callCount = 0;
        guardMock.Setup(g => g.EnsureCanCopyNextFile())
            .Callback(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    var ex = new InvalidOperationException("blocked");
                    ex.Data["errorKey"] = BackupRuntimeKeys.ErrorBusinessSoftwareRunning;
                    ex.Data["0"] = "calc";
                    throw ex;
                }
            });
        var gate = CreateGate(
            stateWriter: stateWriterMock.Object,
            logger: loggerMock.Object,
            executionGuard: guardMock.Object);

        gate.WaitUntilBusinessSoftwareAllowsCopy(
            _job,
            "/src/a.txt",
            "/dst/a.txt",
            2,
            20,
            2,
            20,
            CancellationToken.None);

        stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Blocked)), Times.Once);
        loggerMock.Verify(l => l.Write(It.Is<LogEntry>(entry =>
            entry.EventType == LogEventType.BusinessSoftwareDetected &&
            entry.DestinationPathUNC == "calc")), Times.Once);
    }

    [Fact]
    public void IsBusinessSoftwareBlocked_ReturnsExpectedValue()
    {
        var gate = CreateGate();
        var blocked = new InvalidOperationException();
        blocked.Data["errorKey"] = BackupRuntimeKeys.ErrorBusinessSoftwareRunning;
        var other = new InvalidOperationException();
        other.Data["errorKey"] = "different";

        Assert.True(gate.IsBusinessSoftwareBlocked(blocked));
        Assert.False(gate.IsBusinessSoftwareBlocked(other));
    }

    private BackupRuntimeGate CreateGate(
        IBackupExecutionController? executionController = null,
        IPriorityFilesBarrier? priorityBarrier = null,
        ILargeFileTransferBarrier? largeBarrier = null,
        IBackupExecutionGuard? executionGuard = null,
        IStateWriter? stateWriter = null,
        ILogger? logger = null)
    {
        var reporter = new BackupExecutionReporter(
            stateWriter ?? Mock.Of<IStateWriter>(),
            logger ?? Mock.Of<ILogger>());
        return new BackupRuntimeGate(
            executionController ?? new NoOpBackupExecutionController(),
            priorityBarrier ?? new NoOpPriorityFilesBarrier(),
            largeBarrier ?? new NoOpLargeFileTransferBarrier(),
            executionGuard ?? new NoOpBackupExecutionGuard(),
            reporter);
    }

    private sealed class ActionQueueExecutionController(IEnumerable<string> actions) : IBackupExecutionController
    {
        private readonly Queue<string> _actions = new(actions);

        public void BeginJob(int jobId) { }
        public void EndJob(int jobId) { }
        public void PauseAll() { }
        public void Pause(int jobId) { }
        public void ResumeAll() { }
        public void Resume(int jobId) { }
        public void RequestStopAll() { }
        public void RequestStop(int jobId) { }
        public void WaitIfPausedOrThrowIfStopped() { }

        public bool TryDequeueAction(out string actionKey)
        {
            if (_actions.Count == 0)
            {
                actionKey = string.Empty;
                return false;
            }

            actionKey = _actions.Dequeue();
            return true;
        }

        public bool TryGetCurrentJobControlState(int jobId, out BackupJobControlState controlState)
        {
            controlState = BackupJobControlState.Running;
            return true;
        }
    }

    private sealed class SequenceLargeFileTransferBarrier(params LargeFileTransferAcquireResult[] sequence) : ILargeFileTransferBarrier
    {
        private readonly Queue<LargeFileTransferAcquireResult> _sequence = new(sequence);

        public LargeFileTransferAcquireResult TryAcquire(long fileSizeBytes, long thresholdBytes, out IDisposable? lease)
        {
            lease = null;
            if (_sequence.Count == 0)
            {
                return LargeFileTransferAcquireResult.NotRequired;
            }

            var value = _sequence.Dequeue();
            if (value == LargeFileTransferAcquireResult.Acquired)
            {
                lease = Mock.Of<IDisposable>();
            }

            return value;
        }
    }
}
