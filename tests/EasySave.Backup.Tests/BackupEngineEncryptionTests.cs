using EasySave.Core;
using EasySave.Log;
using EasySave.State;
using EasySave.System;
using Moq;

namespace EasySave.Backup.Tests;

public class BackupEngineEncryptionTests
{
    private readonly Mock<IFileSystem> _fileSystemMock = new();
    private readonly Mock<ITransferService> _transferServiceMock = new();
    private readonly Mock<IStateWriter> _stateWriterMock = new();
    private readonly Mock<ILogger> _loggerMock = new();
    private readonly Mock<IEncryptionPolicyProvider> _policyProviderMock = new();
    private readonly Mock<IEncryptionProviderResolver> _providerResolverMock = new();
    private readonly Mock<IEncryptionProvider> _encryptionProviderMock = new();

    [Fact]
    public void Execute_WhenEncryptionFails_BackupContinuesAndLogsNegativeEncryptionTime()
    {
        var job = new BackupJob
        {
            Id = 42,
            Name = "EncryptedJob",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive(job.Source, It.IsAny<IEnumerable<string>>()))
            .Returns(["/source/report.txt"]);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(120);
        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(true);

        _transferServiceMock.Setup(ts => ts.TransferFile("/source/report.txt", "/destination/report.txt", true))
            .Returns(new TransferResult(120, 7, 0));

        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);
        _policyProviderMock.Setup(p => p.GetPolicy()).Returns(policy);

        _providerResolverMock.Setup(r => r.Resolve(EncryptionProviderNames.DotNet))
            .Returns(_encryptionProviderMock.Object);

        _encryptionProviderMock
            .Setup(p => p.EncryptAsync("/destination/report.txt", policy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EncryptionResult.Failure(-5, 999, "crypto error"));

        var engine = BackupEngineFactory.Create(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            _policyProviderMock.Object,
            _providerResolverMock.Object);

        engine.Execute(job).GetAwaiter().GetResult();

        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(entry =>
            entry.EventType == LogEventType.TransferFile &&
            entry.EncryptionTimeMs < 0)), Times.Once);

        _stateWriterMock.Verify(sw => sw.Update(It.Is<StateEntry>(se => se.Status == BackupStatus.Done)), Times.Once);
        _stateWriterMock.Verify(sw => sw.MarkInactive(job.Id), Times.Once);
    }

    [Fact]
    public void Execute_WhenExtensionIsNotConfigured_DoesNotCallEncryptionProvider()
    {
        var job = new BackupJob
        {
            Id = 7,
            Name = "NoEncJob",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive(job.Source, It.IsAny<IEnumerable<string>>()))
            .Returns(["/source/image.png"]);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(120);
        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(true);

        _transferServiceMock.Setup(ts => ts.TransferFile("/source/image.png", "/destination/image.png", true))
            .Returns(new TransferResult(120, 7, 0));

        _policyProviderMock.Setup(p => p.GetPolicy())
            .Returns(new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null));

        var engine = BackupEngineFactory.Create(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            _policyProviderMock.Object,
            _providerResolverMock.Object);

        engine.Execute(job).GetAwaiter().GetResult();

        _providerResolverMock.Verify(r => r.Resolve(It.IsAny<string>()), Times.Never);
        _encryptionProviderMock.Verify(
            p => p.EncryptAsync(It.IsAny<string>(), It.IsAny<EncryptionPolicy>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(entry =>
            entry.EventType == LogEventType.TransferFile &&
            entry.EncryptionTimeMs == 0)), Times.Once);
    }

    [Fact]
    public void Execute_WhenProviderIsNotFound_BackupContinuesAndLogsNegativeEncryptionTime()
    {
        var job = new BackupJob
        {
            Id = 8,
            Name = "MissingProviderJob",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive(job.Source, It.IsAny<IEnumerable<string>>()))
            .Returns(["/source/report.txt"]);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(120);
        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(true);

        _transferServiceMock.Setup(ts => ts.TransferFile("/source/report.txt", "/destination/report.txt", true))
            .Returns(new TransferResult(120, 7, 0));

        _policyProviderMock.Setup(p => p.GetPolicy())
            .Returns(new EncryptionPolicy([".txt"], EncryptionProviderNames.External, null));

        _providerResolverMock.Setup(r => r.Resolve(EncryptionProviderNames.External))
            .Returns((IEncryptionProvider?)null);

        var engine = BackupEngineFactory.Create(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            _policyProviderMock.Object,
            _providerResolverMock.Object);

        engine.Execute(job).GetAwaiter().GetResult();

        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(entry =>
            entry.EventType == LogEventType.TransferFile &&
            entry.EncryptionTimeMs < 0)), Times.Once);
        _stateWriterMock.Verify(sw => sw.MarkInactive(job.Id), Times.Once);
    }

    [Fact]
    public void Execute_WhenProviderThrows_BackupContinuesAndLogsNegativeEncryptionTime()
    {
        var job = new BackupJob
        {
            Id = 9,
            Name = "ProviderThrowsJob",
            Source = "/source",
            Destination = "/destination",
            Type = BackupType.Complete
        };

        _fileSystemMock.Setup(fs => fs.EnumerateFilesRecursive(job.Source, It.IsAny<IEnumerable<string>>()))
            .Returns(["/source/report.txt"]);
        _fileSystemMock.Setup(fs => fs.GetFileSize(It.IsAny<string>()))
            .Returns(120);
        _fileSystemMock.Setup(fs => fs.DirectoryExists("/destination"))
            .Returns(true);

        _transferServiceMock.Setup(ts => ts.TransferFile("/source/report.txt", "/destination/report.txt", true))
            .Returns(new TransferResult(120, 7, 0));

        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);
        _policyProviderMock.Setup(p => p.GetPolicy()).Returns(policy);

        _providerResolverMock.Setup(r => r.Resolve(EncryptionProviderNames.DotNet))
            .Returns(_encryptionProviderMock.Object);

        _encryptionProviderMock
            .Setup(p => p.EncryptAsync("/destination/report.txt", policy, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("unexpected encryption crash"));

        var engine = BackupEngineFactory.Create(
            _fileSystemMock.Object,
            _transferServiceMock.Object,
            _stateWriterMock.Object,
            _loggerMock.Object,
            _policyProviderMock.Object,
            _providerResolverMock.Object);

        engine.Execute(job).GetAwaiter().GetResult();

        _loggerMock.Verify(l => l.Write(It.Is<LogEntry>(entry =>
            entry.EventType == LogEventType.TransferFile &&
            entry.EncryptionTimeMs < 0)), Times.Once);
        _stateWriterMock.Verify(sw => sw.MarkInactive(job.Id), Times.Once);
    }
}
