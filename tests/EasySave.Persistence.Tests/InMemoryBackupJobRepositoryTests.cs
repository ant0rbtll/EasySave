using EasySave.Core;
using Moq;

namespace EasySave.Persistence.Tests;

public class InMemoryBackupJobRepositoryTests
{
    private readonly Mock<IJobIdProvider> _idProviderMock;

    public InMemoryBackupJobRepositoryTests()
    {
        _idProviderMock = new Mock<IJobIdProvider>();
    }

    private InMemoryBackupJobRepository CreateRepository()
    {
        return new InMemoryBackupJobRepository(_idProviderMock.Object);
    }

    #region Add Tests

    [Fact]
    public void Add_NewJob_AssignsIdFromProvider()
    {
        // Arrange
        _idProviderMock.Setup(p => p.NextId(It.IsAny<List<BackupJob>>())).Returns(1);
        var repo = CreateRepository();
        var job = new BackupJob
        {
            Name = "Test",
            Source = "/src",
            Destination = "/dst",
            Type = BackupType.Complete
        };

        // Act
        repo.Add(job);

        // Assert
        Assert.Equal(1, repo.Count());
        Assert.Equal(1, job.Id);
        _idProviderMock.Verify(p => p.NextId(It.IsAny<List<BackupJob>>()), Times.Once);
    }

    [Fact]
    public void Add_JobWithExistingId_DoesNotCallIdProvider()
    {
        // Arrange
        var repo = CreateRepository();
        var job = new BackupJob
        {
            Id = 42,
            Name = "Test",
            Source = "/src",
            Destination = "/dst"
        };

        // Act
        repo.Add(job);

        // Assert
        Assert.Equal(42, job.Id);
        _idProviderMock.Verify(p => p.NextId(It.IsAny<List<BackupJob>>()), Times.Never);
    }

    [Fact]
    public void Add_MultipleTimes_CallsIdProviderForEach()
    {
        // Arrange
        var callCount = 0;
        _idProviderMock.Setup(p => p.NextId(It.IsAny<List<BackupJob>>()))
            .Returns(() => ++callCount);
        var repo = CreateRepository();

        // Act
        repo.Add(new BackupJob { Name = "Job1", Source = "/src1", Destination = "/dst1" });
        repo.Add(new BackupJob { Name = "Job2", Source = "/src2", Destination = "/dst2" });

        // Assert
        Assert.Equal(2, repo.Count());
        _idProviderMock.Verify(p => p.NextId(It.IsAny<List<BackupJob>>()), Times.Exactly(2));
    }

    [Fact]
    public void Add_JobWithDuplicateId_ThrowsException()
    {
        // Arrange
        var repo = CreateRepository();
        var job1 = new BackupJob { Id = 1, Name = "Job1", Source = "/src", Destination = "/dst" };
        var job2 = new BackupJob { Id = 1, Name = "Job2", Source = "/src", Destination = "/dst" };
        repo.Add(job1);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => repo.Add(job2));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public void Add_MoreThanMaxJobs_ThrowsException()
    {
        // Arrange
        var callCount = 0;
        _idProviderMock.Setup(p => p.NextId(It.IsAny<List<BackupJob>>()))
            .Returns(() => ++callCount);
        var repo = CreateRepository();

        // Act - Ajouter 5 jobs (limite)
        for (int i = 0; i < 5; i++)
        {
            repo.Add(new BackupJob
            {
                Name = $"Job{i}",
                Source = "/src",
                Destination = "/dst",
                Type = BackupType.Complete
            });
        }

        // Assert - Le 6eme doit echouer
        var ex = Assert.Throws<InvalidOperationException>(() =>
            repo.Add(new BackupJob { Name = "Job6", Source = "/src", Destination = "/dst" }));
        Assert.Contains("Cannot add more than", ex.Message);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_ExistingJob_DecreasesCount()
    {
        // Arrange
        _idProviderMock.Setup(p => p.NextId(It.IsAny<List<BackupJob>>())).Returns(1);
        var repo = CreateRepository();
        var job = new BackupJob { Name = "Test", Source = "/src", Destination = "/dst" };
        repo.Add(job);

        // Act
        repo.Remove(job.Id);

        // Assert
        Assert.Equal(0, repo.Count());
    }

    [Fact]
    public void Remove_NonExistentJob_ThrowsException()
    {
        // Arrange
        var repo = CreateRepository();

        // Act & Assert
        var ex = Assert.Throws<KeyNotFoundException>(() => repo.Remove(999));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Remove_ThenAddAgain_AllowsSameId()
    {
        // Arrange
        var repo = CreateRepository();
        var job = new BackupJob { Id = 1, Name = "Test", Source = "/src", Destination = "/dst" };
        repo.Add(job);
        repo.Remove(1);

        // Act
        var newJob = new BackupJob { Id = 1, Name = "NewTest", Source = "/src", Destination = "/dst" };
        repo.Add(newJob);

        // Assert
        Assert.Equal(1, repo.Count());
        Assert.Equal("NewTest", repo.GetById(1).Name);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public void GetById_ExistingJob_ReturnsJob()
    {
        // Arrange
        _idProviderMock.Setup(p => p.NextId(It.IsAny<List<BackupJob>>())).Returns(1);
        var repo = CreateRepository();
        var job = new BackupJob { Name = "Test", Source = "/src", Destination = "/dst" };
        repo.Add(job);

        // Act
        var result = repo.GetById(job.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal("/src", result.Source);
        Assert.Equal("/dst", result.Destination);
    }

    [Fact]
    public void GetById_NonExistentJob_ThrowsException()
    {
        // Arrange
        var repo = CreateRepository();

        // Act & Assert
        var ex = Assert.Throws<KeyNotFoundException>(() => repo.GetById(999));
        Assert.Contains("not found", ex.Message);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public void GetAll_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        var repo = CreateRepository();

        // Act
        var result = repo.GetAll();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAll_WithJobs_ReturnsAllJobsOrderedById()
    {
        // Arrange
        var repo = CreateRepository();
        repo.Add(new BackupJob { Id = 3, Name = "Job3", Source = "/src", Destination = "/dst" });
        repo.Add(new BackupJob { Id = 1, Name = "Job1", Source = "/src", Destination = "/dst" });
        repo.Add(new BackupJob { Id = 2, Name = "Job2", Source = "/src", Destination = "/dst" });

        // Act
        var result = repo.GetAll();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
        Assert.Equal(3, result[2].Id);
    }

    [Fact]
    public void GetAll_ReturnsNewListInstance()
    {
        // Arrange
        _idProviderMock.Setup(p => p.NextId(It.IsAny<List<BackupJob>>())).Returns(1);
        var repo = CreateRepository();
        repo.Add(new BackupJob { Name = "Job1", Source = "/src", Destination = "/dst" });

        // Act
        var result1 = repo.GetAll();
        var result2 = repo.GetAll();

        // Assert - Les deux listes sont differentes instances
        Assert.NotSame(result1, result2);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ExistingJob_UpdatesAllFields()
    {
        // Arrange
        _idProviderMock.Setup(p => p.NextId(It.IsAny<List<BackupJob>>())).Returns(1);
        var repo = CreateRepository();
        var job = new BackupJob
        {
            Name = "Original",
            Source = "/original/src",
            Destination = "/original/dst",
            Type = BackupType.Complete
        };
        repo.Add(job);

        // Act
        var updatedJob = new BackupJob
        {
            Id = job.Id,
            Name = "Updated",
            Source = "/updated/src",
            Destination = "/updated/dst",
            Type = BackupType.Differential
        };
        repo.Update(updatedJob);

        // Assert
        var result = repo.GetById(job.Id);
        Assert.Equal("Updated", result.Name);
        Assert.Equal("/updated/src", result.Source);
        Assert.Equal("/updated/dst", result.Destination);
        Assert.Equal(BackupType.Differential, result.Type);
    }

    [Fact]
    public void Update_NonExistentJob_ThrowsException()
    {
        // Arrange
        var repo = CreateRepository();
        var job = new BackupJob
        {
            Id = 999,
            Name = "Test",
            Source = "/src",
            Destination = "/dst"
        };

        // Act & Assert
        var ex = Assert.Throws<KeyNotFoundException>(() => repo.Update(job));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Update_DoesNotChangeOtherJobs()
    {
        // Arrange
        var repo = CreateRepository();
        repo.Add(new BackupJob { Id = 1, Name = "Job1", Source = "/src1", Destination = "/dst1" });
        repo.Add(new BackupJob { Id = 2, Name = "Job2", Source = "/src2", Destination = "/dst2" });

        // Act
        repo.Update(new BackupJob { Id = 1, Name = "Updated", Source = "/new", Destination = "/new" });

        // Assert
        var job2 = repo.GetById(2);
        Assert.Equal("Job2", job2.Name);
        Assert.Equal("/src2", job2.Source);
    }

    [Fact]
    public void Update_DoesNotChangeCount()
    {
        // Arrange
        _idProviderMock.Setup(p => p.NextId(It.IsAny<List<BackupJob>>())).Returns(1);
        var repo = CreateRepository();
        repo.Add(new BackupJob { Name = "Original", Source = "/src", Destination = "/dst" });
        var initialCount = repo.Count();

        // Act
        repo.Update(new BackupJob { Id = 1, Name = "Updated", Source = "/src", Destination = "/dst" });

        // Assert
        Assert.Equal(initialCount, repo.Count());
    }

    #endregion

    #region Count and MaxJobs Tests

    [Fact]
    public void Count_EmptyRepository_ReturnsZero()
    {
        // Arrange
        var repo = CreateRepository();

        // Act & Assert
        Assert.Equal(0, repo.Count());
    }

    [Fact]
    public void Count_WithJobs_ReturnsCorrectCount()
    {
        // Arrange
        var callCount = 0;
        _idProviderMock.Setup(p => p.NextId(It.IsAny<List<BackupJob>>()))
            .Returns(() => ++callCount);
        var repo = CreateRepository();
        repo.Add(new BackupJob { Name = "Job1", Source = "/src", Destination = "/dst" });
        repo.Add(new BackupJob { Name = "Job2", Source = "/src", Destination = "/dst" });

        // Act & Assert
        Assert.Equal(2, repo.Count());
    }

    [Fact]
    public void Count_AfterRemove_ReturnsCorrectCount()
    {
        // Arrange
        var callCount = 0;
        _idProviderMock.Setup(p => p.NextId(It.IsAny<List<BackupJob>>()))
            .Returns(() => ++callCount);
        var repo = CreateRepository();
        repo.Add(new BackupJob { Name = "Job1", Source = "/src", Destination = "/dst" });
        repo.Add(new BackupJob { Name = "Job2", Source = "/src", Destination = "/dst" });
        repo.Remove(1);

        // Act & Assert
        Assert.Equal(1, repo.Count());
    }

    [Fact]
    public void MaxJobs_Always_Returns5()
    {
        // Arrange
        var repo = CreateRepository();

        // Act & Assert
        Assert.Equal(5, repo.MaxJobs());
    }

    #endregion
}
