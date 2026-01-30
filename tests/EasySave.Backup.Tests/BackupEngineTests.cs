using EasySave.Core;
using EasySave.State;
using EasySave.System;
using EasyLog;
using Moq;

namespace EasySave.Backup.Tests;

public class BackupEngineTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<ITransferService> _transferServiceMock;
    private readonly Mock<IStateWriter> _stateWriterMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly BackupEngine _backupEngine;

    public BackupEngineTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _transferServiceMock = new Mock<ITransferService>();
        _stateWriterMock = new Mock<IStateWriter>();
        _loggerMock = new Mock<ILogger>();

        _backupEngine = new BackupEngine(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public void Execute_CompleteBackup_CopiesAllFiles()
    {
        // Arrange
        var job = new SaveWork
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        var files = new List<string>
        {
            "/source/file1.txt",
            "/source/folder/file2.txt",
            "/source/folder/subfolder/file3.txt"
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFiles("/source"))
            .Returns(["/source/file1.txt"]);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories("/source"))
            .Returns(["/source/folder"]);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles("/source/folder"))
            .Returns(["/source/folder/file2.txt"]);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories("/source/folder"))
            .Returns(["/source/folder/subfolder"]);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles("/source/folder/subfolder"))
            .Returns(["/source/folder/subfolder/file3.txt"]);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories("/source/folder/subfolder"))
            .Returns([]);

        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new TransferResult { FileSizeBytes = 1000, TransferTimeMs = 10, ErrorCode = 0 });

        // Act
        _backupEngine.Execute(job);

        // Assert
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination"), Times.Once);
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination/folder"), Times.Once);
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination/folder/subfolder"), Times.Once);

        _transferServiceMock.Verify(ts => ts.TransferFile("/source/file1.txt", "/destination/file1.txt"), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/folder/file2.txt", "/destination/folder/file2.txt"), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/folder/subfolder/file3.txt", "/destination/folder/subfolder/file3.txt"), Times.Once);

        // Vérifier que l'état a été mis à jour
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.status == BackupStatus.Active)), Times.AtLeastOnce);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.status == BackupStatus.Done)), Times.Once);
    }

    [Fact]
    public void Execute_CompleteBackup_CreatesDirectoriesBeforeCopyingFiles()
    {
        // Arrange
        var job = new SaveWork
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFiles("/source"))
            .Returns(["/source/file.txt"]);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories("/source"))
            .Returns([]);

        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new TransferResult { FileSizeBytes = 1000 });

        // Act
        _backupEngine.Execute(job);

        // Assert
        _fileSystemMock.Verify(fs => fs.DirectoryExists("/destination"), Times.Once);
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination"), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/file.txt", "/destination/file.txt"), Times.Once);
    }

    [Fact]
    public void Execute_DifferentialBackup_CopiesOnlyNewOrModifiedFiles()
    {
        // Arrange
        var job = new SaveWork
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Differential
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFiles("/source"))
            .Returns(["/source/new.txt", "/source/modified.txt", "/source/unchanged.txt"]);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories("/source"))
            .Returns([]);

        // Le fichier new.txt n'existe pas dans la destination
        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.FileExists("/destination/new.txt"))
            .Returns(false);

        // Le fichier modified.txt existe mais a une taille différente
        _fileSystemMock.Setup(fs => fs.FileExists("/destination/modified.txt"))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/modified.txt"))
            .Returns(2000);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/destination/modified.txt"))
            .Returns(1000);

        // Le fichier unchanged.txt existe et a la même taille
        _fileSystemMock.Setup(fs => fs.FileExists("/destination/unchanged.txt"))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/unchanged.txt"))
            .Returns(1000);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/destination/unchanged.txt"))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new TransferResult { FileSizeBytes = 1000 });

        // Act
        _backupEngine.Execute(job);

        // Assert
        // new.txt et modified.txt doivent être copiés
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/new.txt", "/destination/new.txt"), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/modified.txt", "/destination/modified.txt"), Times.Once);

        // unchanged.txt ne doit PAS être copié
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/unchanged.txt", "/destination/unchanged.txt"), Times.Never);

        // Au total, seulement 2 fichiers doivent être transférés
        _transferServiceMock.Verify(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public void Execute_DifferentialBackup_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var job = new SaveWork
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Differential
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFiles("/source"))
            .Returns(["/source/file.txt"]);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories("/source"))
            .Returns([]);

        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new TransferResult { FileSizeBytes = 1000 });

        // Act
        _backupEngine.Execute(job);

        // Assert
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination"), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/file.txt", "/destination/file.txt"), Times.Once);
    }

    [Fact]
    public void Execute_DifferentialBackup_HandlesNestedDirectories()
    {
        // Arrange
        var job = new SaveWork
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Differential
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFiles("/source"))
            .Returns([]);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories("/source"))
            .Returns(["/source/folder"]);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles("/source/folder"))
            .Returns(["/source/folder/file.txt"]);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories("/source/folder"))
            .Returns([]);

        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination/folder"))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.FileExists("/destination/folder/file.txt"))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new TransferResult { FileSizeBytes = 1000 });

        // Act
        _backupEngine.Execute(job);

        // Assert
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination/folder"), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/folder/file.txt", "/destination/folder/file.txt"), Times.Once);
    }

    [Fact]
    public void Execute_CompleteBackup_WithEmptySource_DoesNotCopyAnything()
    {
        // Arrange
        var job = new SaveWork
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFiles("/source"))
            .Returns([]);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories("/source"))
            .Returns([]);

        // Act
        _backupEngine.Execute(job);

        // Assert
        _transferServiceMock.Verify(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        
        // Vérifier que l'état a été mis à jour même sans fichiers
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.status == BackupStatus.Active)), Times.Once);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.status == BackupStatus.Done)), Times.Once);
    }
}