namespace EasySave.UI.Tests;

public class JobEditSessionServiceTests
{
    [Fact]
    public void HasPendingChanges_ReturnsTrueWhenJobIsModified()
    {
        var service = new JobEditSessionService();
        var job = new BackupJob { Id = 1, Name = "A", Source = "S", Destination = "D", Type = BackupType.Complete };

        service.BeginOrRefresh(job);
        job.Name = "B";

        Assert.True(service.HasPendingChanges(job));
    }

    [Fact]
    public void Restore_RevertsJobToSnapshot()
    {
        var service = new JobEditSessionService();
        var job = new BackupJob { Id = 2, Name = "A", Source = "S", Destination = "D", Type = BackupType.Complete };

        service.BeginOrRefresh(job);
        job.Name = "Changed";
        service.Restore(job);

        Assert.Equal("A", job.Name);
        Assert.False(service.HasPendingChanges(job));
    }

    [Fact]
    public void Clear_ResetsTrackedSession()
    {
        var service = new JobEditSessionService();
        var job = new BackupJob { Id = 3, Name = "A", Source = "S", Destination = "D", Type = BackupType.Complete };

        service.BeginOrRefresh(job);
        service.Clear();
        job.Name = "Changed";

        Assert.False(service.HasPendingChanges(job));
    }
}
