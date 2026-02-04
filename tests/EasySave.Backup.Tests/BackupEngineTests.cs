using EasySave.Core;
using EasySave.State;
using EasySave.System;
using EasySave.Log;
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
        var job = new BackupJob
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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source"))
            .Returns(files);

        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job);

        // Assert
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination"), Times.Once);
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination/folder"), Times.Once);
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination/folder/subfolder"), Times.Once);

        _transferServiceMock.Verify(ts => ts.TransferFile("/source/file1.txt", "/destination/file1.txt", true), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/folder/file2.txt", "/destination/folder/file2.txt", true), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/folder/subfolder/file3.txt", "/destination/folder/subfolder/file3.txt", true), Times.Once);

        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Active)), Times.AtLeastOnce);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Done)), Times.Once);
    }

    [Fact]
    public void Execute_CompleteBackup_CreatesDirectoriesBeforeCopyingFiles()
    {
        // Arrange
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source"))
            .Returns(new List<string> { "/source/file.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 0, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job);

        // Assert
        _fileSystemMock.Verify(fs => fs.DirectoryExists("/destination"), Times.Once);
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination"), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/file.txt", "/destination/file.txt", true), Times.Once);
    }

    [Fact]
    public void Execute_DifferentialBackup_CopiesOnlyNewOrModifiedFiles()
    {
        // Arrange
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Differential
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source"))
            .Returns(new List<string> { "/source/new.txt", "/source/modified.txt", "/source/unchanged.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.FileExists("/destination/new.txt"))
            .Returns(false);

        _fileSystemMock.Setup(fs => fs.FileExists("/destination/modified.txt"))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/modified.txt"))
            .Returns(2000);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/destination/modified.txt"))
            .Returns(1000);

        _fileSystemMock.Setup(fs => fs.FileExists("/destination/unchanged.txt"))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/unchanged.txt"))
            .Returns(1000);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/destination/unchanged.txt"))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 0, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job);

        // Assert
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/new.txt", "/destination/new.txt", true), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/modified.txt", "/destination/modified.txt", true), Times.Once);

        _transferServiceMock.Verify(ts => ts.TransferFile("/source/unchanged.txt", "/destination/unchanged.txt", true), Times.Never);

        _transferServiceMock.Verify(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true), Times.Exactly(2));
    }

    [Fact]
    public void Execute_DifferentialBackup_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Differential
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source"))
            .Returns(new List<string> { "/source/file.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 0, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job);

        // Assert
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination"), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/file.txt", "/destination/file.txt", true), Times.Once);
    }

    [Fact]
    public void Execute_DifferentialBackup_HandlesNestedDirectories()
    {
        // Arrange
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Differential
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source"))
            .Returns(new List<string> { "/source/folder/file.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination/folder"))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.FileExists("/destination/folder/file.txt"))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 0, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job);

        // Assert
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/destination/folder"), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/folder/file.txt", "/destination/folder/file.txt", true), Times.Once);
    }

    [Fact]
    public void Execute_CompleteBackup_WithEmptySource_DoesNotCopyAnything()
    {
        // Arrange
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source"))
            .Returns(new List<string>());

        // Act
        _backupEngine.Execute(job);

        // Assert
        _transferServiceMock.Verify(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true), Times.Never);

        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Active)), Times.Once);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Done)), Times.Once);
    }

    [Fact]
    public void Execute_CompleteBackup_UpdatesStateWithCorrectProgress()
    {
        // Arrange
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source"))
            .Returns(new List<string> { "/source/file1.txt", "/source/file2.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job);

        // Assert
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => 
            se.BackupId == 1 &&
            se.BackupName == "TestBackup" &&
            se.Status == BackupStatus.Active &&
            se.TotalFiles == 2 &&
            se.TotalSizeBytes == 2000 &&
            se.RemainingFiles == 2 &&
            se.RemainingSizeBytes == 2000 &&
            se.ProgressPercent == 0
        )), Times.Once);

        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => 
            se.Status == BackupStatus.Done &&
            se.RemainingFiles == 0 &&
            se.RemainingSizeBytes == 0 &&
            se.ProgressPercent == 100
        )), Times.Once);
    }

    [Fact]
    public void Execute_CompleteBackup_LogsFileTransfers()
    {
        // Arrange
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source"))
            .Returns(new List<string> { "/source/file.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 50, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job);

        // Assert
        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(le => 
            le.BackupName == "TestBackup" &&
            le.EventType == LogEventType.TransferFile &&
            le.SourcePathUNC == "/source/file.txt" &&
            le.DestinationPathUNC == "/destination/file.txt" &&
            le.FileSizeBytes == 1000 &&
            le.TransferTimeMs == 50
        )), Times.Once);
    }

    [Fact]
    public void Execute_CompleteBackup_LogsDirectoryCreation()
    {
        // Arrange
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source"))
            .Returns(new List<string> { "/source/folder/file.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job);

        // Assert
        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(le => 
            le.BackupName == "TestBackup" &&
            le.EventType == LogEventType.CreateDirectory &&
            le.SourcePathUNC == "/destination/folder"
        )), Times.Once);
    }

    [Fact]
    public void Execute_WhenExceptionThrown_UpdatesStateToError()
    {
        // Arrange
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source"))
            .Throws(new Exception("Test exception"));

        // Act & Assert
        Assert.Throws<Exception>(() => _backupEngine.Execute(job));

        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se =>
            se.Status == BackupStatus.Error
        )), Times.Once);

        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(le =>
            le.EventType == LogEventType.Error
        )), Times.Once);
    }
}
