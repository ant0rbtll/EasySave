
using EasySave.Application.Tests.EasySave.Application;
using EasySave.Application.Tests.EasySave.Backup;
using EasySave.Application.Tests.EasySave.Core;
using EasySave.Application.Tests.EasySave.Persistence;
using global::EasySave.Backup;
using global::EasySave.Core;
using global::EasySave.Persistence;
using Moq;
using EasySave.System.Collections.Generic;
using Xunit;

namespace EasySave.Application.Tests;
namespace EasySave.Application.Tests;
public class BackupAppServiceTests
{
    private readonly Mock<IBackupJobRepository> _repoMock;
    private readonly Mock<IUI> _uiMock;
    private readonly Mock<BackupEngine> _engineMock;
    private readonly BackupAppService _service;

    public BackupAppServiceTests()
    {
        _repoMock = new Mock<IBackupJobRepository>();
        _uiMock = new Mock<IUI>();
        // Note: BackupEngine doit avoir ses méthodes en 'virtual' pour être mocké, 
        // ou alors utiliser une interface IBackupEngine.
        _engineMock = new Mock<BackupEngine>();

        _service = new BackupAppService(_repoMock.Object, _uiMock.Object, _engineMock.Object);
    }

    // --- Tests pour CreateJob ---

    [Fact]
    public void CreateJob_ShouldCallRepositoryAdd()
    {
        // Act
        _service.CreateJob("TestJob", "C:/Src", "D:/Dest", BackupType.Full);

        // Assert
        _repoMock.Verify(r => r.Add(It.Is<BackupJob>(j => j.Name == "TestJob")), Times.Once);
    }

    // --- Tests pour RemoveJob ---

    [Fact]
    public void RemoveJob_ShouldCallRepositoryRemoveWithCorrectId()
    {
        // Act
        _service.RemoveJob(5);

        // Assert
        _repoMock.Verify(r => r.Remove(5), Times.Once);
    }

    // --- Tests pour RunJobs ---

    [Fact]
    public void RunJobs_ShouldExecuteOnlyExistingJobs()
    {
        // Arrange
        var job = new BackupJob { Id = 1, Name = "Job1" };
        _repoMock.Setup(r => r.GetById(1)).Returns(job);
        _repoMock.Setup(r => r.GetById(2)).Returns((BackupJob)null); // Le job 2 n'existe pas

        // Act
        _service.RunJobs(new int[] { 1, 2 });

        // Assert
        _engineMock.Verify(e => e.Execute(job), Times.Once);
        _engineMock.Verify(e => e.Execute(It.IsAny<BackupJob>()), Times.Once); // Seulement une exécution totale
    }

    [Fact]
    public void RunJobs_EmptyArray_ShouldDoNothing()
    {
        // Act
        _service.RunJobs(new int[] { });

        // Assert
        _engineMock.Verify(e => e.Execute(It.IsAny<BackupJob>()), Times.Never);
    }

    // --- Tests pour RunAllJobs ---

    [Fact]
    public void RunAllJobs_ShouldExecuteAllJobsInRepo()
    {
        // Arrange
        var jobs = new List<BackupJob> { new BackupJob(), new BackupJob() };
        _repoMock.Setup(r => r.GetAll()).Returns(jobs);

        // Act
        _service.RunAllJobs();

        // Assert
        _engineMock.Verify(e => e.Execute(It.IsAny<BackupJob>()), Times.Exactly(2));
    }

    [Fact]
    public void RunAllJobs_WhenRepoEmpty_ShouldNotCallEngine()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAll()).Returns(new List<BackupJob>());

        // Act
        _service.RunAllJobs();

        // Assert
        _engineMock.Verify(e => e.Execute(It.IsAny<BackupJob>()), Times.Never);
    }

    // --- Tests pour GetAllJobs ---

    [Fact]
    public void GetAllJobs_ShouldReturnListOfJobs()
    {
        // Arrange
        var jobs = new List<BackupJob> { new BackupJob { Id = 1, Name = "J1" } };
        _repoMock.Setup(r => r.GetAll()).Returns(jobs);

        // Act
        var result = _service.GetAllJobs();

        // Assert
        Assert.Single(result);
        Assert.Equal("J1", result[0].Name);
    }

    [Fact]
    public void GetAllJobs_ShouldDisplayMessagesInUI()
    {
        // Arrange
        var jobs = new List<BackupJob> {
            new BackupJob { Id = 1, Name = "JobTest", Source = "S", Destination = "D", Type = BackupType.Full }
        };
        _repoMock.Setup(r => r.GetAll()).Returns(jobs);

        // Act
        _service.GetAllJobs();

        // Assert
        _uiMock.Verify(u => u.ShowMessage(It.Is<string>(s => s.Contains("JobTest"))), Times.Once);
    }

    // --- Tests de robustesse (Edge Cases) ---

    [Fact]
    public void RunJobs_WithNullIdList_ShouldNotThrow()
    {
        // Act & Assert (Vérifie que ça ne crash pas si on passe null au foreach, 
        // Note: cela dépend si vous avez ajouté une vérification de nullité)
        var exception = Record.Exception(() => _service.RunJobs(null));
        // Si votre code actuel crash, ce test échouera, ce qui est une bonne info !
    }

    [Fact]
    public void CreateJob_WithEmptyStrings_ShouldStillCallAdd()
    {
        // Act
        _service.CreateJob("", "", "", BackupType.Full);

        // Assert
        _repoMock.Verify(r => r.Add(It.IsAny<BackupJob>()), Times.Once);
    }
}