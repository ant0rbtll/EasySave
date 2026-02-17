using EasySave.Core;
using EasySave.State;

namespace EasySave.Application.Tests;

public class BackupJobStateServiceTests
{
    [Fact]
    public void ApplyState_WithMatchingEntry_UpdatesRuntimeFields()
    {
        var timestamp = new DateTime(2026, 2, 11, 10, 30, 0, DateTimeKind.Utc);
        var reader = new StubStateReader(new Dictionary<int, StateEntry>
        {
            [42] = new()
            {
                BackupId = 42,
                Timestamp = timestamp,
                Status = BackupStatus.Active
            }
        });

        var service = new BackupJobStateService(reader);
        var job = new BackupJob { Id = 42 };

        service.ApplyState(job);

        Assert.Equal(timestamp, job.LastExecutionDate);
        Assert.True(job.IsActive);
        Assert.Equal(BackupJobStatus.Active, job.Status);
    }

    [Fact]
    public void ApplyState_WithPausedEntry_MapsPausedAndKeepsJobActive()
    {
        var timestamp = new DateTime(2026, 2, 11, 10, 30, 0, DateTimeKind.Utc);
        var reader = new StubStateReader(new Dictionary<int, StateEntry>
        {
            [42] = new()
            {
                BackupId = 42,
                Timestamp = timestamp,
                Status = BackupStatus.Paused
            }
        });

        var service = new BackupJobStateService(reader);
        var job = new BackupJob { Id = 42 };

        service.ApplyState(job);

        Assert.Equal(timestamp, job.LastExecutionDate);
        Assert.True(job.IsActive);
        Assert.Equal(BackupJobStatus.Paused, job.Status);
    }

    [Fact]
    public void ApplyState_WithMissingEntry_ResetsRuntimeFields()
    {
        var reader = new StubStateReader(new Dictionary<int, StateEntry>());
        var service = new BackupJobStateService(reader);
        var job = new BackupJob
        {
            Id = 42,
            LastExecutionDate = DateTime.UtcNow,
            IsActive = true
        };

        service.ApplyState(job);

        Assert.Null(job.LastExecutionDate);
        Assert.False(job.IsActive);
        Assert.Equal(BackupJobStatus.Inactive, job.Status);
    }

    [Fact]
    public void ApplyState_OnCollection_LoadsStateOnceAndUpdatesAllJobs()
    {
        var reader = new StubStateReader(new Dictionary<int, StateEntry>
        {
            [1] = new()
            {
                BackupId = 1,
                Timestamp = new DateTime(2026, 2, 11, 9, 0, 0, DateTimeKind.Utc),
                Status = BackupStatus.Active
            }
        });

        var service = new BackupJobStateService(reader);
        var jobs = new List<BackupJob>
        {
            new() { Id = 1 },
            new() { Id = 2, LastExecutionDate = DateTime.UtcNow, IsActive = true }
        };

        service.ApplyState(jobs);

        Assert.Equal(1, reader.ReadEntriesCalls);
        Assert.True(jobs[0].IsActive);
        Assert.Equal(BackupJobStatus.Active, jobs[0].Status);
        Assert.False(jobs[1].IsActive);
        Assert.Equal(BackupJobStatus.Inactive, jobs[1].Status);
        Assert.Null(jobs[1].LastExecutionDate);
    }

    [Fact]
    public void ApplyState_WithWaitingStatus_MapsToWaitingJobStatus()
    {
        var reader = new StubStateReader(new Dictionary<int, StateEntry>
        {
            [10] = new()
            {
                BackupId = 10,
                Timestamp = new DateTime(2026, 2, 17, 8, 0, 0, DateTimeKind.Utc),
                Status = BackupStatus.Waiting
            }
        });

        var service = new BackupJobStateService(reader);
        var job = new BackupJob { Id = 10 };

        service.ApplyState(job);

        Assert.False(job.IsActive);
        Assert.Equal(BackupJobStatus.Waiting, job.Status);
    }

    private sealed class StubStateReader(IReadOnlyDictionary<int, StateEntry> entries) : IStateReader
    {
        public int ReadEntriesCalls { get; private set; }

        public IReadOnlyDictionary<int, StateEntry> ReadEntries()
        {
            ReadEntriesCalls++;
            return entries;
        }
    }
}
