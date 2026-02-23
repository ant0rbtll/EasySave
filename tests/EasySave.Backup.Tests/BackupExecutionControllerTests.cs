using EasySave.Backup;
using EasySave.Exceptions;

namespace EasySave.Backup.Tests;

public class BackupExecutionControllerTests
{
    [Fact]
    public void WaitIfPausedOrThrowIfStopped_WhenPaused_ShouldBlockUntilResume()
    {
        using var controller = new BackupExecutionController();
        controller.BeginJob(1);
        controller.PauseAll();

        var hasAction = controller.TryDequeueAction(out var actionKey);
        Assert.True(hasAction);
        Assert.Equal("action_backup_paused_by_user", actionKey);

        var waitTask = Task.Run(() => controller.WaitIfPausedOrThrowIfStopped());
        Assert.False(waitTask.Wait(TimeSpan.FromMilliseconds(150)));

        controller.ResumeAll();
        var completed = waitTask.Wait(TimeSpan.FromSeconds(2));

        Assert.True(completed);
    }

    [Fact]
    public void WaitIfPausedOrThrowIfStopped_WhenStopRequested_ShouldThrowLocalizedException()
    {
        using var controller = new BackupExecutionController();
        controller.BeginJob(1);
        controller.RequestStopAll();

        var ex = Assert.Throws<EasysaveDefaultException>(() => controller.WaitIfPausedOrThrowIfStopped());

        Assert.Equal(Localization.LocalizationKey.error_backup_stopped_by_user, ex.ErrorKey);
        Assert.Equal("action_backup_stopped_by_user", ex.Options[0]);
    }

    [Fact]
    public void PauseForJob_WhenMultipleJobsRunning_ShouldPauseOnlyTargetJob()
    {
        using var controller = new BackupExecutionController();
        controller.BeginJob(1);
        controller.BeginJob(2);

        controller.Pause(1);

        Assert.True(controller.TryGetCurrentJobControlState(1, out var job1State));
        Assert.True(controller.TryGetCurrentJobControlState(2, out var job2State));
        Assert.Equal(BackupJobControlState.Paused, job1State);
        Assert.Equal(BackupJobControlState.Running, job2State);
    }

    [Fact]
    public void RequestStopForJob_WhenMultipleJobsRunning_ShouldStopOnlyTargetJob()
    {
        using var controller = new BackupExecutionController();
        controller.BeginJob(1);
        controller.BeginJob(2);

        controller.RequestStop(1);

        Assert.True(controller.TryGetCurrentJobControlState(1, out var job1State));
        Assert.True(controller.TryGetCurrentJobControlState(2, out var job2State));
        Assert.Equal(BackupJobControlState.StopRequested, job1State);
        Assert.Equal(BackupJobControlState.Running, job2State);
    }

    [Fact]
    public void PauseForJob_BeforeBeginJob_ShouldApplyWhenJobStarts()
    {
        using var controller = new BackupExecutionController();
        controller.Pause(42);

        Assert.True(controller.TryGetCurrentJobControlState(42, out var pendingState));
        Assert.Equal(BackupJobControlState.Paused, pendingState);

        controller.BeginJob(42);

        var hasAction = controller.TryDequeueAction(out var actionKey);
        Assert.True(hasAction);
        Assert.Equal("action_backup_paused_by_user", actionKey);
        Assert.True(controller.TryGetCurrentJobControlState(42, out var startedState));
        Assert.Equal(BackupJobControlState.Paused, startedState);
    }

    [Fact]
    public void RequestStopForJob_BeforeBeginJob_ShouldApplyWhenJobStarts()
    {
        using var controller = new BackupExecutionController();
        controller.RequestStop(42);

        Assert.True(controller.TryGetCurrentJobControlState(42, out var pendingState));
        Assert.Equal(BackupJobControlState.StopRequested, pendingState);

        controller.BeginJob(42);

        var ex = Assert.Throws<EasysaveDefaultException>(() => controller.WaitIfPausedOrThrowIfStopped());
        Assert.Equal(Localization.LocalizationKey.error_backup_stopped_by_user, ex.ErrorKey);
        Assert.Equal("action_backup_stopped_by_user", ex.Options[0]);
    }

    [Fact]
    public void ResumeForJob_BeforeBeginJob_ShouldClearPendingPause()
    {
        using var controller = new BackupExecutionController();
        controller.Pause(42);
        controller.Resume(42);

        Assert.False(controller.TryGetCurrentJobControlState(42, out _));

        controller.BeginJob(42);

        Assert.True(controller.TryGetCurrentJobControlState(42, out var startedState));
        Assert.Equal(BackupJobControlState.Running, startedState);
        Assert.False(controller.TryDequeueAction(out _));
    }
}
