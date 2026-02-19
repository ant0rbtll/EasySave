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
    private readonly Mock<IEncryptionPolicyProvider> _encryptionPolicyProviderMock;
    private readonly Mock<IEncryptionProviderResolver> _encrytionResolverProviderMock;
    private readonly Mock<IBackupExecutionGuard> _backupUpExecutionGuardMock;
    private readonly BackupEngine _backupEngine;

    public BackupEngineTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _transferServiceMock = new Mock<ITransferService>();
        _stateWriterMock = new Mock<IStateWriter>();
        _loggerMock = new Mock<ILogger>();
        _encryptionPolicyProviderMock = new Mock<IEncryptionPolicyProvider>();
        _encrytionResolverProviderMock = new Mock<IEncryptionProviderResolver>();
        _backupUpExecutionGuardMock = new Mock<IBackupExecutionGuard>();
        _encryptionPolicyProviderMock
            .Setup(p => p.GetPolicy())
            .Returns(EncryptionPolicy.Disabled);

        _backupEngine = new BackupEngine(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            _encryptionPolicyProviderMock.Object,
            _encrytionResolverProviderMock.Object,
            _backupUpExecutionGuardMock.Object
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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(files);

        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 0, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
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
        _backupEngine.Execute(job).GetAwaiter().GetResult();

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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 0, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
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
        _backupEngine.Execute(job).GetAwaiter().GetResult();

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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string>());

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file1.txt", "/source/file2.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 50, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/folder/file.txt" });

        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

        // Assert
        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(le => 
            le.BackupName == "TestBackup" &&
            le.EventType == LogEventType.CreateDirectory &&
            le.SourcePathUNC == "/destination/folder"
        )), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenExceptionThrown_UpdatesStateToError()
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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Throws(new Exception("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _backupEngine.Execute(job));

        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se =>
            se.Status == BackupStatus.Error
        )), Times.Once);

        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(le =>
            le.EventType == LogEventType.Error
        )), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenBusinessSoftwareDetectedDuringFileLoop_WaitsThenContinuesAndLogsReason()
    {
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file1.txt", "/source/file2.txt" });
        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);
        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        var guardMock = new Mock<IBackupExecutionGuard>();
        int callCount = 0;
        guardMock.Setup(g => g.EnsureCanCopyNextFile())
            .Callback(() =>
            {
                callCount++;
                if (callCount is 2 or 3)
                {
                    var ex = new InvalidOperationException("error_business_software_running");
                    ex.Data["errorKey"] = "error_business_software_running";
                    ex.Data["0"] = "calc";
                    throw ex;
                }
            });

        var engine = new BackupEngine(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            executionGuard: guardMock.Object);

        await engine.Execute(job);

        _transferServiceMock.Verify(ts => ts.TransferFile("/source/file1.txt", "/destination/file1.txt", true), Times.Once);
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/file2.txt", "/destination/file2.txt", true), Times.Once);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Error)), Times.Never);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Done)), Times.Once);
        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(le =>
            le.EventType == LogEventType.BusinessSoftwareDetected &&
            le.SourcePathUNC == "error_business_software_running" &&
            le.DestinationPathUNC == "calc")), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenCancelledDuringBusinessSoftwareWait_ShouldThrowOperationCanceledException()
    {
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file1.txt" });
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        var guardMock = new Mock<IBackupExecutionGuard>();
        guardMock.Setup(g => g.EnsureCanCopyNextFile())
            .Throws(() =>
            {
                var ex = new InvalidOperationException("error_business_software_running");
                ex.Data["errorKey"] = "error_business_software_running";
                ex.Data["0"] = "calc";
                return ex;
            });

        var engine = new BackupEngine(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            executionGuard: guardMock.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(80));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => engine.Execute(job, cts.Token));

        _transferServiceMock.Verify(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true), Times.Never);
    }

    [Fact]
    public async Task Execute_WhenPauseActionIsPending_LogsActionEventWithActionKey()
    {
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file1.txt" });
        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);
        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        var executionController = new StubExecutionController(
            actions: ["action_backup_paused_by_user"]);

        var engine = new BackupEngine(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            executionController: executionController);

        await engine.Execute(job);

        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(le =>
            le.EventType == LogEventType.Action &&
            le.SourcePathUNC == "action_backup_paused_by_user")), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenControllerReportsPaused_UpdatesStateToPausedThenActive()
    {
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file1.txt" });
        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);
        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        var executionController = new StubExecutionController(
            controlStates: [BackupJobControlState.Paused, BackupJobControlState.Running]);

        var engine = new BackupEngine(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            executionController: executionController);

        await engine.Execute(job);

        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Paused)), Times.AtLeastOnce);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Active)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Execute_WhenControllerReportsPaused_ExcludesAndReincludesPriorityFilesForThatJob()
    {
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file1.txt" });
        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);
        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        var barrierMock = new Mock<IPriorityFilesBarrier>();
        var executionController = new StubExecutionController(
            controlStates: [BackupJobControlState.Paused, BackupJobControlState.Running]);

        var engine = new BackupEngine(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            priorityFilesBarrier: barrierMock.Object,
            executionController: executionController);

        await engine.Execute(job);

        barrierMock.Verify(b => b.PauseJob(job.Id), Times.AtLeastOnce);
        barrierMock.Verify(b => b.ResumeJob(job.Id), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Execute_WhenStoppedByUser_MarksInactiveAndLogsStoppedEvent()
    {
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file1.txt" });
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        var stopException = new InvalidOperationException("error_backup_stopped_by_user");
        stopException.Data["errorKey"] = "error_backup_stopped_by_user";
        stopException.Data["actionKey"] = "action_backup_stopped_by_user";
        var executionController = new StubExecutionController(waitException: stopException);

        var engine = new BackupEngine(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            executionController: executionController);

        var exThrown = await Assert.ThrowsAsync<InvalidOperationException>(() => engine.Execute(job));
        Assert.Equal("error_backup_stopped_by_user", exThrown.Message);

        _stateWriterMock.Verify(sw => sw.MarkInactive(1), Times.Once);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Error)), Times.Never);
        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(le =>
            le.EventType == LogEventType.Stopped &&
            le.SourcePathUNC == "action_backup_stopped_by_user")), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenStopRequestedDuringPriorityWait_ShouldStopWithoutWaitingBarrierCompletion()
    {
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file1.txt" });
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        var neverCompleted = new TaskCompletionSource<bool>();
        var barrierMock = new Mock<IPriorityFilesBarrier>();
        barrierMock
            .Setup(b => b.WaitUntilNoPriorityPendingAsync(It.IsAny<CancellationToken>()))
            .Returns(neverCompleted.Task);

        var executionController = new StopOnSecondWaitExecutionController();
        var engine = new BackupEngine(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            priorityFilesBarrier: barrierMock.Object,
            executionController: executionController);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => engine.Execute(job));
        Assert.Equal(BackupRuntimeKeys.ErrorBackupStoppedByUser, ex.Message);

        _transferServiceMock.Verify(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true), Times.Never);
        _stateWriterMock.Verify(sw => sw.MarkInactive(1), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenStopRequestedDuringLargeFileSlotWait_ShouldStopWithoutWaitingSlotCompletion()
    {
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/large.bin" });
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(20 * 1024);

        var executionController = new StopOnSecondWaitExecutionController();
        var alwaysBusyLargeBarrier = new AlwaysBusyLargeFileTransferBarrier();
        var engine = new BackupEngine(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            executionController: executionController,
            largeFileTransferBarrier: alwaysBusyLargeBarrier);

        var executionContext = new BackupExecutionContext(parallelLargeFileThresholdBytes: 1024);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => engine.Execute(job, executionContext: executionContext));
        Assert.Equal(BackupRuntimeKeys.ErrorBackupStoppedByUser, ex.Message);

        _transferServiceMock.Verify(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true), Times.Never);
        _stateWriterMock.Verify(sw => sw.MarkInactive(1), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenResumedWhileStillWaitingForPriorityBarrier_ShouldReturnToWaitingStatus()
    {
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file1.txt" });
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        var neverCompleted = new TaskCompletionSource<bool>();
        var barrierMock = new Mock<IPriorityFilesBarrier>();
        barrierMock
            .Setup(b => b.WaitUntilNoPriorityPendingAsync(It.IsAny<CancellationToken>()))
            .Returns(neverCompleted.Task);

        var executionController = new StubExecutionController(
            controlStates: [BackupJobControlState.Running, BackupJobControlState.Paused, BackupJobControlState.Running]);

        var engine = new BackupEngine(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            priorityFilesBarrier: barrierMock.Object,
            executionController: executionController);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(260));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => engine.Execute(job, cts.Token));

        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Waiting)), Times.AtLeast(2));
    }

    [Fact]
    public async Task Execute_UnsupportedBackupType_ThrowsNotSupportedException()
    {
        // Arrange
        var job = new BackupJob
        {
            Id = 1,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = (BackupType)999 // Unsupported type
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file.txt" });
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        // Act & Assert
        
        await Assert.ThrowsAsync<NotSupportedException>(() => _backupEngine.Execute(job));
    }

    private sealed class StubExecutionController(
        IEnumerable<string>? actions = null,
        Exception? waitException = null,
        IEnumerable<BackupJobControlState>? controlStates = null) : IBackupExecutionController
    {
        private readonly Queue<string> _actions = new(actions ?? []);
        private readonly Exception? _waitException = waitException;
        private readonly Queue<BackupJobControlState> _controlStates = new(controlStates ?? []);

        public void BeginJob(int jobId) { }

        public void EndJob(int jobId) { }

        public void PauseAll() { }

        public void Pause(int jobId) { }

        public void ResumeAll() { }

        public void Resume(int jobId) { }

        public void RequestStopAll() { }

        public void RequestStop(int jobId) { }

        public void WaitIfPausedOrThrowIfStopped()
        {
            if (_waitException is not null)
                throw _waitException;
        }

        public bool TryDequeueAction(out string actionKey)
        {
            if (_actions.Count == 0)
            {
                actionKey = string.Empty;
                return false;
            }

            actionKey = _actions.Dequeue();
            return true;
        }

        public bool TryGetCurrentJobControlState(int jobId, out BackupJobControlState controlState)
        {
            if (_controlStates.Count == 0)
            {
                controlState = default;
                return false;
            }

            controlState = _controlStates.Dequeue();
            return true;
        }
    }

    private sealed class StopOnSecondWaitExecutionController : IBackupExecutionController
    {
        private int _waitCalls;

        public void BeginJob(int jobId) { }
        public void EndJob(int jobId) { }
        public void PauseAll() { }
        public void Pause(int jobId) { }
        public void ResumeAll() { }
        public void Resume(int jobId) { }
        public void RequestStopAll() { }
        public void RequestStop(int jobId) { }

        public void WaitIfPausedOrThrowIfStopped()
        {
            _waitCalls++;
            if (_waitCalls < 2)
                return;

            var exception = new InvalidOperationException(BackupRuntimeKeys.ErrorBackupStoppedByUser);
            exception.Data["errorKey"] = BackupRuntimeKeys.ErrorBackupStoppedByUser;
            exception.Data["actionKey"] = BackupRuntimeKeys.ActionBackupStoppedByUser;
            throw exception;
        }

        public bool TryDequeueAction(out string actionKey)
        {
            actionKey = string.Empty;
            return false;
        }

        public bool TryGetCurrentJobControlState(int jobId, out BackupJobControlState controlState)
        {
            controlState = BackupJobControlState.Running;
            return true;
        }
    }

    private sealed class AlwaysBusyLargeFileTransferBarrier : ILargeFileTransferBarrier
    {
        public LargeFileTransferAcquireResult TryAcquire(long fileSizeBytes, long thresholdBytes, out IDisposable? lease)
        {
            lease = null;
            return fileSizeBytes > thresholdBytes && thresholdBytes > 0
                ? LargeFileTransferAcquireResult.Busy
                : LargeFileTransferAcquireResult.NotRequired;
        }
    }

    [Fact]
    public async Task Execute_TransferFails_ThrowsInvalidOperationException()
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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file.txt" });
        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 0, TransferTimeMs: 0, ErrorCode: 1)); // Error code != 0

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _backupEngine.Execute(job));
        Assert.Contains("File transfer failed", ex.Message);
        Assert.Contains("error code 1", ex.Message);

    }

    [Fact]
    public void Execute_CompleteBackup_CallsMarkInactiveAtEnd()
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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file.txt" });
        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

        // Assert
        _stateWriterMock.Verify(sw => sw.MarkInactive(1), Times.Once);
    }

    [Fact]
    public void Execute_DifferentialBackup_CallsMarkInactiveAtEnd()
    {
        // Arrange
        var job = new BackupJob
        {
            Id = 2,
            Name = "TestBackup",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Differential
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string>());

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

        // Assert
        _stateWriterMock.Verify(sw => sw.MarkInactive(2), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenExceptionThrown_DoesNotCallMarkInactive()
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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Throws(new Exception("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _backupEngine.Execute(job));

        _stateWriterMock.Verify(sw => sw.MarkInactive(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void Execute_CompleteBackup_WithZeroSizeFile_CopiesFile()
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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/empty.txt" });
        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(0);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 0, TransferTimeMs: 1, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

        // Assert
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/empty.txt", "/destination/empty.txt", true), Times.Once);
    }

    [Fact]
    public void Execute_DifferentialBackup_SameSizeFiles_DoesNotCopy()
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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file.txt" });
        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.FileExists("/destination/file.txt"))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/file.txt"))
            .Returns(1000);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/destination/file.txt"))
            .Returns(1000);

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

        // Assert
        _transferServiceMock.Verify(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void Execute_DifferentialBackup_FileNotExistsInDestination_CopiesFile()
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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/newfile.txt" });
        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.FileExists("/destination/newfile.txt"))
            .Returns(false);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

        // Assert
        _transferServiceMock.Verify(ts => ts.TransferFile("/source/newfile.txt", "/destination/newfile.txt", true), Times.Once);
    }

    [Fact]
    public void Execute_CompleteBackup_MultipleFiles_UpdatesProgressCorrectly()
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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file1.txt", "/source/file2.txt", "/source/file3.txt", "/source/file4.txt" });
        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 10, ErrorCode: 0));

        // Act
        _backupEngine.Execute(job).GetAwaiter().GetResult();

        // Assert - Check that progress updates happened
        // Note: 100% is called twice (after last file + final Done state)
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.ProgressPercent == 25)), Times.Once);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.ProgressPercent == 50)), Times.Once);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.ProgressPercent == 75)), Times.Once);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.ProgressPercent == 100 && se.Status == BackupStatus.Active)), Times.Once);
        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.ProgressPercent == 100 && se.Status == BackupStatus.Done)), Times.Once);
    }

    [Fact]
    public async Task Execute_TransferFails_UpdatesStateToError()
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

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive("/source", It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "/source/file.txt" });
        _fileSystemMock.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(1000);

        _transferServiceMock.Setup(ts => ts.TransferFile(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(new TransferResult(FileSizeBytes: 0, TransferTimeMs: 0, ErrorCode: 5));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _backupEngine.Execute(job));

        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Error)), Times.Once);
    }
}
