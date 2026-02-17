using EasySave.Backup;

namespace EasySave.Backup.Tests;

public class BackupExecutionControllerTests
{
    [Fact]
    public void WaitIfPausedOrThrowIfStopped_WhenPaused_ShouldBlockUntilResume()
    {
        using var controller = new BackupExecutionController();
        controller.BeginJob(1);
        controller.Pause();

        var hasAction = controller.TryDequeueAction(out var actionKey);
        Assert.True(hasAction);
        Assert.Equal("action_backup_paused_by_user", actionKey);

        var waitTask = Task.Run(() => controller.WaitIfPausedOrThrowIfStopped());
        Assert.False(waitTask.Wait(TimeSpan.FromMilliseconds(150)));

        controller.Resume();
        var completed = waitTask.Wait(TimeSpan.FromSeconds(2));

        Assert.True(completed);
    }

    [Fact]
    public void WaitIfPausedOrThrowIfStopped_WhenStopRequested_ShouldThrowLocalizedException()
    {
        using var controller = new BackupExecutionController();
        controller.BeginJob(1);
        controller.RequestStop();

        var ex = Assert.Throws<InvalidOperationException>(() => controller.WaitIfPausedOrThrowIfStopped());

        Assert.Equal("error_backup_stopped_by_user", ex.Message);
        Assert.Equal("error_backup_stopped_by_user", ex.Data["errorKey"]);
        Assert.Equal("action_backup_stopped_by_user", ex.Data["actionKey"]);
    }

    [Fact]
    public void PauseForJob_WhenMultipleJobsRunning_ShouldPauseOnlyTargetJob()
    {
        using var controller = new BackupExecutionController();
        controller.BeginJob(1);
        controller.BeginJob(2);

        controller.PauseForJob(1);

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

        controller.RequestStopForJob(1);

        Assert.True(controller.TryGetCurrentJobControlState(1, out var job1State));
        Assert.True(controller.TryGetCurrentJobControlState(2, out var job2State));
        Assert.Equal(BackupJobControlState.StopRequested, job1State);
        Assert.Equal(BackupJobControlState.Running, job2State);
    }
}
