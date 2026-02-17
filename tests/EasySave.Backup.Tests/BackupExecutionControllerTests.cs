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
}
