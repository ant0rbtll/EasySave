using Moq;
using EasySave.Backup;
using EasySave.Persistence;
using EasySave.Core;
using EasySave.State;
using EasySave.System;

namespace EasySave.Application.Tests;

public class BackupApplicationServiceTests
{
    private readonly Mock<IBackupJobRepository> _repoMock;
    private readonly Mock<IBackupEngine> _engineMock;
    private readonly Mock<IBackupExecutionGuard> _backupExecutionGuardMock;
    private readonly Mock<IBackupJobStateService> _backupJobStateServiceMock;
    private readonly Mock<IStateReader> _stateReaderMock;
    private readonly BackupApplicationService _service;

    public BackupApplicationServiceTests()
    {
        _repoMock = new Mock<IBackupJobRepository>();
        _engineMock = new Mock<IBackupEngine>();
        _backupExecutionGuardMock = new Mock<IBackupExecutionGuard>();
        _backupJobStateServiceMock = new Mock<IBackupJobStateService>();
        _stateReaderMock = new Mock<IStateReader>();
        _stateReaderMock.Setup(r => r.ReadEntries()).Returns(new Dictionary<int, StateEntry>());
        _service = new BackupApplicationService(_repoMock.Object, _engineMock.Object, _backupJobStateServiceMock.Object, _stateReaderMock.Object);
    }

    /// <summary>
    /// Verifies that CreateJob correctly maps arguments to a BackupJob object and calls the repository's Add method.
    /// </summary>
    [Fact]
    public void CreateJob_ShouldCallAddOnRepository_WithCorrectData()
    {
        string name = "DailyBackup";
        string src = "C:/Data";
        string dest = "D:/Backup";
        BackupType type = BackupType.Complete;

        _service.CreateJob(name, src, dest, type);

        _repoMock.Verify(r => r.Add(It.Is<BackupJob>(j =>
            j.Name == name &&
            j.Source == src &&
            j.Destination == dest &&
            j.Type == type
        )), Times.Once);
    }

    /// <summary>
    /// Ensures that RemoveJob invokes the repository's Remove method with the specified identifier.
    /// </summary>
    [Fact]
    public void RemoveJob_ShouldCallRemoveOnRepository_WithCorrectId()
    {
        int idToRemove = 5;

        _service.RemoveJob(idToRemove);

        _repoMock.Verify(r => r.Remove(idToRemove), Times.Once);
    }

    /// <summary>
    /// Checks if GetAllJobs returns the exact list of jobs provided by the repository.
    /// </summary>
    [Fact]
    public void GetAllJobs_ShouldReturnJobsFromRepository()
    {
        var expectedJobs = new List<BackupJob>
        {
            new BackupJob { Name = "Job1" },
            new BackupJob { Name = "Job2" }
        };
        _repoMock.Setup(r => r.GetAll()).Returns(expectedJobs);

        var result = _service.GetAllJobs();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Job1", result[0].Name);
        _backupJobStateServiceMock.Verify(s => s.ApplyState(It.IsAny<IEnumerable<BackupJob>>()), Times.Once);
    }

    [Fact]
    public void GetAllJobsRuntimeStates_ShouldReturnStateForEachJob()
    {
        var jobs = new List<BackupJob>
        {
            new() { Id = 1, Status = BackupJobStatus.Active, IsActive = true },
            new() { Id = 2, Status = BackupJobStatus.Inactive, IsActive = false }
        };
        _repoMock.Setup(r => r.GetAll()).Returns(jobs);

        var result = _service.GetAllJobsRuntimeStates();

        Assert.Equal(2, result.Count);
        Assert.Equal(BackupJobStatus.Active, result[1].Status);
        Assert.True(result[1].IsActive);
        Assert.Equal(BackupJobStatus.Inactive, result[2].Status);
        Assert.False(result[2].IsActive);
        _backupJobStateServiceMock.Verify(s => s.ApplyState(It.IsAny<IEnumerable<BackupJob>>()), Times.Once);
    }

    [Fact]
    public void GetAllJobsLiveProgress_ShouldReturnOnlyActiveEntries()
    {
        var timestamp = new DateTime(2026, 2, 16, 11, 45, 0, DateTimeKind.Utc);
        _stateReaderMock.Setup(r => r.ReadEntries()).Returns(new Dictionary<int, StateEntry>
        {
            [1] = new()
            {
                BackupId = 1,
                BackupName = "Job One",
                Status = BackupStatus.Active,
                ProgressPercent = 42,
                TotalFiles = 100,
                TotalSizeBytes = 1000,
                RemainingFiles = 58,
                RemainingSizeBytes = 580,
                CurrentSourcePath = "/src/a.txt",
                CurrentDestinationPath = "/dst/a.txt",
                Timestamp = timestamp
            },
            [2] = new()
            {
                BackupId = 2,
                BackupName = "Job Two",
                Status = BackupStatus.Done,
                ProgressPercent = 100
            }
        });

        var result = _service.GetAllJobsLiveProgress();

        Assert.Single(result);
        Assert.Equal(BackupJobStatus.Active, result[1].Status);
        Assert.Equal("Job One", result[1].Name);
        Assert.Equal(42, result[1].ProgressPercent);
        Assert.Equal(100, result[1].TotalFiles);
        Assert.Equal(1000, result[1].TotalSizeBytes);
        Assert.Equal(58, result[1].RemainingFiles);
        Assert.Equal(580, result[1].RemainingSizeBytes);
        Assert.Equal("/src/a.txt", result[1].CurrentSourcePath);
        Assert.Equal("/dst/a.txt", result[1].CurrentDestinationPath);
        Assert.Equal(timestamp, result[1].LastUpdateAt);
        Assert.False(result.ContainsKey(2));
    }

    [Fact]
    public void GetAllJobsLiveProgress_ShouldClampProgressPercent()
    {
        _stateReaderMock.Setup(r => r.ReadEntries()).Returns(new Dictionary<int, StateEntry>
        {
            [7] = new()
            {
                BackupId = 7,
                BackupName = "Job Seven",
                Status = BackupStatus.Active,
                ProgressPercent = 140
            }
        });

        var result = _service.GetAllJobsLiveProgress();

        Assert.Equal(100, result[7].ProgressPercent);
    }

    [Fact]
    public void GetAllJobsLiveProgress_ShouldUseDictionaryKeyAsId_AndResolveNonEmptyName()
    {
        _repoMock.Setup(r => r.GetAll()).Returns(
        [
            new BackupJob { Id = 10, Name = "Repo Name" }
        ]);
        _stateReaderMock.Setup(r => r.ReadEntries()).Returns(new Dictionary<int, StateEntry>
        {
            [10] = new()
            {
                BackupId = 0,
                BackupName = "",
                Status = BackupStatus.Active,
                ProgressPercent = 12
            }
        });

        var result = _service.GetAllJobsLiveProgress();

        Assert.Single(result);
        Assert.True(result.ContainsKey(10));
        Assert.Equal(10, result[10].Id);
        Assert.Equal("Repo Name", result[10].Name);
    }

    /// <summary>
    /// Verifies that GetJob retrieves the expected job when a valid ID is provided.
    /// </summary>
    [Fact]
    public void GetJob_ShouldReturnCorrectJob()
    {
        int jobId = 10;
        var expectedJob = new BackupJob { Name = "SpecificJob" };
        _repoMock.Setup(r => r.GetById(jobId)).Returns(expectedJob);

        var result = _service.GetJob(jobId);

        Assert.NotNull(result);
        Assert.Equal("SpecificJob", result.Name);
        _repoMock.Verify(r => r.GetById(jobId), Times.Once);
        _backupJobStateServiceMock.Verify(s => s.ApplyState(expectedJob), Times.Once);
    }

    /// <summary>
    /// Ensures that UpdateJob correctly forwards the job object to the repository's update method.
    /// </summary>
    [Fact]
    public void UpdateJob_ShouldCallUpdateOnRepository()
    {
        var jobToUpdate = new BackupJob { Name = "UpdatedJob" };

        _service.UpdateJob(jobToUpdate);

        _repoMock.Verify(r => r.Update(jobToUpdate), Times.Once);
    }

    /// <summary>
    /// Verifies that GetJob returns null when the repository cannot find the requested job.
    /// </summary>
    [Fact]
    public void GetJob_WhenJobDoesNotExist_ShouldReturnNull()
    {
        _repoMock.Setup(r => r.GetById(999)).Returns((BackupJob?)null);

        var result = _service.GetJob(999);

        Assert.Null(result);
        _backupJobStateServiceMock.Verify(s => s.ApplyState(It.IsAny<BackupJob>()), Times.Never);
    }

    /// <summary>
    /// Ensures that GetAllJobs returns an empty list rather than null when the repository is empty.
    /// </summary>
    [Fact]
    public void GetAllJobs_WhenNoJobs_ShouldReturnEmptyList()
    {
        _repoMock.Setup(r => r.GetAll()).Returns(new List<BackupJob>());

        var result = _service.GetAllJobs();

        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that attempting to run a non-existent job by ID does not cause an application crash.
    /// </summary>
    [Fact]
    public void RunJob_WhenJobDoesNotExist_ShouldNotCallEngine()
    {
        _repoMock.Setup(r => r.GetById(888)).Returns((BackupJob?)null);

        _service.RunJob(888);

        _engineMock.Verify(e => e.Execute(It.IsAny<BackupJob>()), Times.Never);
    }

    /// <summary>
    /// Confirms that the service still attempts to register a job even if input strings are empty.
    /// </summary>
    [Fact]
    public void CreateJob_WithEmptyStrings_ShouldStillCallRepository()
    {
        _service.CreateJob("", "", "", BackupType.Differential);

        _repoMock.Verify(r => r.Add(It.Is<BackupJob>(j => j.Name == "")), Times.Once);
    }

    /// <summary>
    /// Ensures that providing an empty array of IDs results in no interactions with the repository.
    /// </summary>
    [Fact]
    public void RunJobs_WithEmptyArray_ShouldNotCallRepository()
    {
        _service.RunJobs(new int[] { });

        _repoMock.Verify(r => r.GetById(It.IsAny<int>()), Times.Never);
        _engineMock.Verify(e => e.Execute(It.IsAny<BackupJob>()), Times.Never);
    }

    /// <summary>
    /// Verifies that RunJobs calls the repository for each ID provided in the array.
    /// </summary>
    [Fact]
    public void RunJobs_ShouldCallRepositoryForEachId()
    {
        int[] ids = { 1, 2, 3 };
        _repoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((BackupJob?)null);

        _service.RunJobs(ids);

        _repoMock.Verify(r => r.GetById(1), Times.Once);
        _repoMock.Verify(r => r.GetById(2), Times.Once);
        _repoMock.Verify(r => r.GetById(3), Times.Once);
    }

    /// <summary>
    /// Ensures that RunAllJobs requests all jobs from the repository exactly once.
    /// </summary>
    [Fact]
    public void RunAllJobs_ShouldRequestAllJobsFromRepository()
    {
        _repoMock.Setup(r => r.GetAll()).Returns(new List<BackupJob>());

        _service.RunAllJobs();

        _repoMock.Verify(r => r.GetAll(), Times.Once);
        _engineMock.Verify(e => e.Execute(It.IsAny<BackupJob>()), Times.Never);
    }

    /// <summary>
    /// Checks that CreateJob correctly handles special or long directory paths.
    /// </summary>
    [Fact]
    public void CreateJob_ShouldHandleLongPathsCorrectly()
    {
        string longPath = new string('a', 255);
        _service.CreateJob("LongPathJob", longPath, "D:/Dest", BackupType.Complete);

        _repoMock.Verify(r => r.Add(It.Is<BackupJob>(j => j.Source == longPath)), Times.Once);
    }

    /// <summary>
    /// Verifies that the service forwards a null job to the repository without intercepting it.
    /// </summary>
    [Fact]
    public void UpdateJob_WithNullJob_ShouldForwardToRepository()
    {
        _service.UpdateJob(null!);

        _repoMock.Verify(r => r.Update(null!), Times.Once);
    }

    /// <summary>
    /// Ensures that RunJobs gracefully ignores IDs that are not found in the repository.
    /// </summary>
    [Fact]
    public void RunJobs_WithMixedValidAndInvalidIds_ShouldContinueProcessing()
    {
        _repoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((BackupJob?)null);

        var exception = Record.Exception(() => _service.RunJobs(new int[] { 1, 99 }));

        Assert.Null(exception);
        _repoMock.Verify(r => r.GetById(1), Times.Once);
        _repoMock.Verify(r => r.GetById(99), Times.Once);
    }

    /// <summary>
    /// Checks that CreateJob can handle duplicate names, assuming the repository handles uniqueness logic.
    /// </summary>
    [Fact]
    public void CreateJob_WithDuplicateName_ShouldStillCallRepository()
    {
        _service.CreateJob("Duplicate", "src", "dst", BackupType.Complete);
        _service.CreateJob("Duplicate", "src2", "dst2", BackupType.Differential);

        _repoMock.Verify(r => r.Add(It.IsAny<BackupJob>()), Times.Exactly(2));
    }

    /// <summary>
    /// Verifies that GetAllJobs returns a new list instance to avoid reference issues.
    /// </summary>
    [Fact]
    public void GetAllJobs_ShouldReturnNotNullInstance()
    {
        _repoMock.Setup(r => r.GetAll()).Returns(new List<BackupJob>());

        var result = _service.GetAllJobs();

        Assert.NotNull(result);
        Assert.IsType<List<BackupJob>>(result);
    }

    /// <summary>
    /// Ensures that RunJob does not call any engine logic if the job returned by repository is null.
    /// </summary>
    [Fact]
    public void RunJob_WithNullResultFromRepo_ShouldNotCallEngine()
    {
        _repoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((BackupJob?)null);

        _service.RunJob(99);

        _repoMock.Verify(r => r.GetById(99), Times.Once);
        _engineMock.Verify(e => e.Execute(It.IsAny<BackupJob>()), Times.Never);
    }

    /// <summary>
    /// Verifies that removing a job with a negative ID is passed to the repository without modification.
    /// </summary>
    [Fact]
    public void RemoveJob_WithNegativeId_ShouldForwardToRepository()
    {
        _service.RemoveJob(-1);

        _repoMock.Verify(r => r.Remove(-1), Times.Once);
    }

    /// <summary>
    /// Ensures that CreateJob correctly assigns the BackupType enum values.
    /// </summary>
    [Fact]
    public void CreateJob_ShouldCorrectlyAssignBackupType()
    {
        _service.CreateJob("TypeTest", "S", "D", BackupType.Differential);

        _repoMock.Verify(r => r.Add(It.Is<BackupJob>(j => j.Type == BackupType.Differential)), Times.Once);
    }

    #region RunJob / RunJobs / RunAllJobs with engine execution

    [Fact]
    public void RunJob_WithExistingJob_ShouldCallEngineExecute()
    {
        var job = new BackupJob { Id = 1, Name = "Test", Source = "/src", Destination = "/dst", Type = BackupType.Complete };
        _repoMock.Setup(r => r.GetById(1)).Returns(job);

        _service.RunJob(1);

        _engineMock.Verify(e => e.Execute(job), Times.Once);
    }

    [Fact]
    public void RunJob_WithMissingJob_ShouldNotCallEngineExecute()
    {
        _repoMock.Setup(r => r.GetById(1)).Returns((BackupJob?)null);

        _service.RunJob(1);

        _engineMock.Verify(e => e.Execute(It.IsAny<BackupJob>()), Times.Never);
    }

    [Fact]
    public void RunJobs_WithExistingJobs_ShouldCallEngineExecuteForEach()
    {
        var job1 = new BackupJob { Id = 1, Name = "Job1", Source = "/src1", Destination = "/dst1", Type = BackupType.Complete };
        var job2 = new BackupJob { Id = 2, Name = "Job2", Source = "/src2", Destination = "/dst2", Type = BackupType.Complete };
        _repoMock.Setup(r => r.GetById(1)).Returns(job1);
        _repoMock.Setup(r => r.GetById(2)).Returns(job2);

        _service.RunJobs(new[] { 1, 2 });

        _engineMock.Verify(e => e.Execute(job1), Times.Once);
        _engineMock.Verify(e => e.Execute(job2), Times.Once);
    }

    [Fact]
    public void RunAllJobs_WithJobs_ShouldCallEngineExecuteForEach()
    {
        var jobs = new List<BackupJob>
        {
            new BackupJob { Id = 1, Name = "Job1", Source = "/src1", Destination = "/dst1", Type = BackupType.Complete },
            new BackupJob { Id = 2, Name = "Job2", Source = "/src2", Destination = "/dst2", Type = BackupType.Complete }
        };
        _repoMock.Setup(r => r.GetAll()).Returns(jobs);

        _service.RunAllJobs();

        _engineMock.Verify(e => e.Execute(jobs[0]), Times.Once);
        _engineMock.Verify(e => e.Execute(jobs[1]), Times.Once);
    }

    [Fact]
    public void RunJob_WhenBusinessSoftwareIsRunning_ShouldBlockExecution()
    {
        var job = new BackupJob { Id = 1, Name = "Test", Source = "/src", Destination = "/dst", Type = BackupType.Complete };
        _repoMock.Setup(r => r.GetById(1)).Returns(job);
        _backupExecutionGuardMock
            .Setup(g => g.EnsureCanCopyNextFile())
            .Throws(() =>
            {
                var ex = new InvalidOperationException("error_business_software_running");
                ex.Data["errorKey"] = "error_business_software_running";
                ex.Data["0"] = "calc";
                return ex;
            });

        var service = new BackupApplicationService(
            _repoMock.Object,
            _engineMock.Object,
            _backupJobStateServiceMock.Object,
            backupExecutionGuard: _backupExecutionGuardMock.Object);

        var exception = Assert.Throws<InvalidOperationException>(() => service.RunJob(1));

        Assert.Equal("error_business_software_running", exception.Message);
        Assert.Equal("error_business_software_running", exception.Data["errorKey"]);
        Assert.Equal("calc", exception.Data["0"]);
        _engineMock.Verify(e => e.Execute(It.IsAny<BackupJob>()), Times.Never);
    }

    [Fact]
    public void RunJob_WhenBusinessSoftwareIsConfiguredButNotRunning_ShouldExecute()
    {
        var job = new BackupJob { Id = 1, Name = "Test", Source = "/src", Destination = "/dst", Type = BackupType.Complete };
        _repoMock.Setup(r => r.GetById(1)).Returns(job);

        var service = new BackupApplicationService(
            _repoMock.Object,
            _engineMock.Object,
            _backupJobStateServiceMock.Object,
            backupExecutionGuard: _backupExecutionGuardMock.Object);

        service.RunJob(1);

        _engineMock.Verify(e => e.Execute(job), Times.Once);
    }

    [Fact]
    public void RunJob_WhenEngineThrowsBusinessSoftwareError_ShouldPropagate()
    {
        var job = new BackupJob { Id = 1, Name = "Test", Source = "/src", Destination = "/dst", Type = BackupType.Complete };
        _repoMock.Setup(r => r.GetById(1)).Returns(job);

        var service = new BackupApplicationService(
            _repoMock.Object,
            _engineMock.Object,
            _backupJobStateServiceMock.Object,
            backupExecutionGuard: _backupExecutionGuardMock.Object);

        _engineMock
            .Setup(e => e.Execute(job))
            .Throws(() =>
            {
                var ex = new InvalidOperationException("error_business_software_running");
                ex.Data["errorKey"] = "error_business_software_running";
                ex.Data["0"] = "calc";
                return ex;
            });

        var exception = Assert.Throws<InvalidOperationException>(() => service.RunJob(1));

        Assert.Equal("error_business_software_running", exception.Message);
        Assert.Equal("error_business_software_running", exception.Data["errorKey"]);
        Assert.Equal("calc", exception.Data["0"]);
        _engineMock.Verify(e => e.Execute(job), Times.Once);
    }

    #endregion
}
