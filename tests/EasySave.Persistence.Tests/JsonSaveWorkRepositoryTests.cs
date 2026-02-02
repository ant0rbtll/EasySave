using EasySave.Configuration;
using EasySave.Core;
using EasySave.Persistence;

namespace EasySave.Persistence.Tests;

public class JsonSaveWorkRepositoryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testFilePath;
    private readonly TestPathProvider _pathProvider;

    public JsonSaveWorkRepositoryTests()
    {
        // Créer un répertoire temporaire pour les tests
        _testDirectory = Path.Combine(Path.GetTempPath(), $"EasySaveTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _testFilePath = Path.Combine(_testDirectory, "jobs.json");
        _pathProvider = new TestPathProvider(_testFilePath);
    }

    public void Dispose()
    {
        // Nettoyer après les tests
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Add_NewJob_CreatesFileAndSavesJob()
    {
        // Arrange
        var repo = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
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
        Assert.True(File.Exists(_testFilePath));
        Assert.Equal(1, repo.Count());
        Assert.Equal(1, job.Id);
    }

    [Fact]
    public void Add_MultipleTimes_PersistsAllJobs()
    {
        // Arrange
        var repo = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        
        // Act
        repo.Add(new SaveWork { Name = "Job1", Source = "/src1", Destination = "/dst1" });
        repo.Add(new SaveWork { Name = "Job2", Source = "/src2", Destination = "/dst2" });
        
        // Assert - Créer une nouvelle instance pour vérifier la persistance
        var repo2 = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        Assert.Equal(2, repo2.Count());
    }

    [Fact]
    public void Add_MoreThanMaxJobs_ThrowsException()
    {
        // Arrange
        var repo = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        
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
    public void Remove_ExistingJob_UpdatesFile()
    {
        // Arrange
        var repo = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        var job = new SaveWork { Name = "Test", Source = "/src", Destination = "/dst" };
        repo.Add(job);
        
        // Act
        repo.Remove(job.Id);
        
        // Assert
        Assert.Equal(0, repo.Count());
        
        // Vérifier la persistance
        var repo2 = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        Assert.Equal(0, repo2.Count());
    }

    [Fact]
    public void Remove_NonExistentJob_ThrowsException()
    {
        // Arrange
        var repo = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => repo.Remove(999));
    }

    [Fact]
    public void GetById_ExistingJob_ReturnsJob()
    {
        // Arrange
        var repo = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        var job = new SaveWork { Name = "Test", Source = "/src", Destination = "/dst" };
        repo.Add(job);
        
        // Act
        var result = repo.GetById(job.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void GetById_NonExistentJob_ThrowsException()
    {
        // Arrange
        var repo = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => repo.GetById(999));
    }

    [Fact]
    public void GetAll_NoFile_ReturnsEmptyList()
    {
        // Arrange
        var repo = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        
        // Act
        var result = repo.GetAll();
        
        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAll_WithJobs_ReturnsAllJobsPersisted()
    {
        // Arrange
        var repo = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        repo.Add(new SaveWork { Name = "Job1", Source = "/src", Destination = "/dst" });
        repo.Add(new SaveWork { Name = "Job2", Source = "/src", Destination = "/dst" });
        
        // Act - Nouvelle instance pour vérifier la persistance
        var repo2 = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        var result = repo2.GetAll();
        
        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Load_CorruptedFile_ReturnsEmptyList()
    {
        // Arrange
        File.WriteAllText(_testFilePath, "{ invalid json }");
        var repo = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        
        // Act
        var result = repo.GetAll();
        
        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void MaxJobs_Always_Returns5()
    {
        // Arrange
        var repo = new JsonSaveWorkRepository(_pathProvider, new SequentialJobIdProvider());
        
        // Act & Assert
        Assert.Equal(5, repo.MaxJobs());
    }

    // Classe helper pour les tests
    private class TestPathProvider : IPathProvider
    {
        private readonly string _jobsPath;

        public TestPathProvider(string jobsPath)
        {
            _jobsPath = jobsPath;
        }

        public string GetDailyLogPath(DateTime date) => throw new NotImplementedException();
        public string GetStatePath() => throw new NotImplementedException();
        public string GetJobsConfigPath() => _jobsPath;
    }
}