using EasySave.Core;
using EasySave.Persistence;

namespace EasySave.Persistence.Tests;

public class InMemorySaveWorkRepositoryTests
{
    [Fact]
    public void Add_NewJob_Success()
    {
        // Arrange
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        var job = new SaveWork
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
    }

    [Fact]
    public void Add_JobWithExistingId_ThrowsException()
    {
        // Arrange
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        var job1 = new SaveWork { Id = 1, Name = "Job1", Source = "/src", Destination = "/dst" };
        var job2 = new SaveWork { Id = 1, Name = "Job2", Source = "/src", Destination = "/dst" };
        repo.Add(job1);
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => repo.Add(job2));
    }

    [Fact]
    public void Add_MoreThanMaxJobs_ThrowsException()
    {
        // Arrange
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        
        // Act - Ajouter 5 jobs (limite)
        for (int i = 0; i < 5; i++)
        {
            repo.Add(new SaveWork 
            { 
                Name = $"Job{i}", 
                Source = "/src", 
                Destination = "/dst",
                Type = BackupType.Complete
            });
        }
        
        // Assert - Le 6ème doit échouer
        Assert.Throws<InvalidOperationException>(() => 
            repo.Add(new SaveWork { Name = "Job6", Source = "/src", Destination = "/dst" }));
    }

    [Fact]
    public void Remove_ExistingJob_Success()
    {
        // Arrange
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        var job = new SaveWork { Name = "Test", Source = "/src", Destination = "/dst" };
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
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => repo.Remove(999));
    }

    [Fact]
    public void GetById_ExistingJob_ReturnsJob()
    {
        // Arrange
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        var job = new SaveWork { Name = "Test", Source = "/src", Destination = "/dst" };
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
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => repo.GetById(999));
    }

    [Fact]
    public void GetAll_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        
        // Act
        var result = repo.GetAll();
        
        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAll_WithJobs_ReturnsAllJobsOrderedById()
    {
        // Arrange
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        repo.Add(new SaveWork { Name = "Job3", Source = "/src", Destination = "/dst" });
        repo.Add(new SaveWork { Name = "Job1", Source = "/src", Destination = "/dst" });
        repo.Add(new SaveWork { Name = "Job2", Source = "/src", Destination = "/dst" });
        
        // Act
        var result = repo.GetAll();
        
        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
        Assert.Equal(3, result[2].Id);
    }

    [Fact]
    public void Count_EmptyRepository_ReturnsZero()
    {
        // Arrange
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        
        // Act & Assert
        Assert.Equal(0, repo.Count());
    }

    [Fact]
    public void Count_WithJobs_ReturnsCorrectCount()
    {
        // Arrange
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        repo.Add(new SaveWork { Name = "Job1", Source = "/src", Destination = "/dst" });
        repo.Add(new SaveWork { Name = "Job2", Source = "/src", Destination = "/dst" });
        
        // Act & Assert
        Assert.Equal(2, repo.Count());
    }

    [Fact]
    public void MaxJobs_Always_Returns5()
    {
        // Arrange
        var repo = new InMemorySaveWorkRepository(new SequentialJobIdProvider());
        
        // Act & Assert
        Assert.Equal(5, repo.MaxJobs());
    }
}