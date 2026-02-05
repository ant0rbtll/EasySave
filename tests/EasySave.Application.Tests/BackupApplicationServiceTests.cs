using Moq;
using EasySave.Backup;
using EasySave.Persistence;
using EasySave.Core;

namespace EasySave.Application.Tests;

public class BackupAppServiceTests
{
    private readonly Mock<IBackupJobRepository> _repoMock;
    private readonly Mock<IBackupEngine> _engineMock;
    private readonly BackupAppService _service;

    public BackupAppServiceTests()
    {
        _repoMock = new Mock<IBackupJobRepository>();
        _engineMock = new Mock<IBackupEngine>();
        _service = new BackupAppService(_repoMock.Object, _engineMock.Object);
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
    }

    /// <summary>
    /// Verifies that GetJobById retrieves the expected job when a valid ID is provided.
    /// </summary>
    [Fact]
    public void GetJobById_ShouldReturnCorrectJob()
    {
        int jobId = 10;
        var expectedJob = new BackupJob { Name = "SpecificJob" };
        _repoMock.Setup(r => r.GetById(jobId)).Returns(expectedJob);

        var result = _service.GetJobById(jobId);

        Assert.NotNull(result);
        Assert.Equal("SpecificJob", result.Name);
        _repoMock.Verify(r => r.GetById(jobId), Times.Once);
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
    /// Verifies that GetJobById returns null when the repository cannot find the requested job.
    /// </summary>
    [Fact]
    public void GetJobById_WhenJobDoesNotExist_ShouldReturnNull()
    {
        _repoMock.Setup(r => r.GetById(999)).Returns((BackupJob?)null);

        var result = _service.GetJobById(999);

        Assert.Null(result);
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
    public void RunJobById_WhenJobDoesNotExist_ShouldNotCallEngine()
    {
        _repoMock.Setup(r => r.GetById(888)).Returns((BackupJob?)null);

        _service.RunJobById(888);

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
    public void RunJobsByIds_WithEmptyArray_ShouldNotCallRepository()
    {
        _service.RunJobsByIds(new int[] { });

        _repoMock.Verify(r => r.GetById(It.IsAny<int>()), Times.Never);
        _engineMock.Verify(e => e.Execute(It.IsAny<BackupJob>()), Times.Never);
    }

    /// <summary>
    /// Verifies that RunJobsByIds calls the repository for each ID provided in the array.
    /// </summary>
    [Fact]
    public void RunJobsByIds_ShouldCallRepositoryForEachId()
    {
        int[] ids = { 1, 2, 3 };
        _repoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((BackupJob?)null);

        _service.RunJobsByIds(ids);

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
    /// Verifies that the service does not throw an exception when updating a null object.
    /// </summary>
    [Fact]
    public void UpdateJob_WithNullJob_ShouldNotThrowException()
    {
        var exception = Record.Exception(() => _service.UpdateJob(null!));
        Assert.Null(exception);
    }

    /// <summary>
    /// Ensures that RunJobsByIds gracefully ignores IDs that are not found in the repository.
    /// </summary>
    [Fact]
    public void RunJobsByIds_WithMixedValidAndInvalidIds_ShouldContinueProcessing()
    {
        _repoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((BackupJob?)null);

        var exception = Record.Exception(() => _service.RunJobsByIds(new int[] { 1, 99 }));

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
    /// Ensures that RunJobById does not call any engine logic if the job returned by repository is null.
    /// </summary>
    [Fact]
    public void RunJobById_WithNullResultFromRepo_ShouldNotCallEngine()
    {
        _repoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((BackupJob?)null);

        _service.RunJobById(99);

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

    #region RunJob / RunJobById / RunJobsByIds / RunAllJobs with engine execution

    [Fact]
    public void RunJob_WithValidJob_ShouldCallEngineExecute()
    {
        var job = new BackupJob { Id = 1, Name = "Test", Source = "/src", Destination = "/dst", Type = BackupType.Complete };

        _service.RunJob(job);

        _engineMock.Verify(e => e.Execute(job), Times.Once);
    }

    [Fact]
    public void RunJobById_WithExistingJob_ShouldCallEngineExecute()
    {
        var job = new BackupJob { Id = 1, Name = "Test", Source = "/src", Destination = "/dst", Type = BackupType.Complete };
        _repoMock.Setup(r => r.GetById(1)).Returns(job);

        _service.RunJobById(1);

        _engineMock.Verify(e => e.Execute(job), Times.Once);
    }

    [Fact]
    public void RunJobsByIds_WithExistingJobs_ShouldCallEngineExecuteForEach()
    {
        var job1 = new BackupJob { Id = 1, Name = "Job1", Source = "/src1", Destination = "/dst1", Type = BackupType.Complete };
        var job2 = new BackupJob { Id = 2, Name = "Job2", Source = "/src2", Destination = "/dst2", Type = BackupType.Complete };
        _repoMock.Setup(r => r.GetById(1)).Returns(job1);
        _repoMock.Setup(r => r.GetById(2)).Returns(job2);

        _service.RunJobsByIds(new[] { 1, 2 });

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

    #endregion
}
