using EasySave.Core;
using EasySave.Exceptions;
using EasySave.Localization;
using EasySave.System;
using Moq;

namespace EasySave.Backup.Tests;

public class BackupFileExecutionServiceTests
{
    private readonly Mock<IFileSystem> _fileSystemMock = new();
    private readonly Mock<ITransferService> _transferServiceMock = new();
    private readonly Mock<IEncryptionProviderResolver> _resolverMock = new();
    private readonly Mock<IEncryptionProvider> _providerMock = new();

    [Fact]
    public void PrepareDestinationDirectory_WhenDirectoryAlreadyExists_DoesNotCreate()
    {
        const string destinationFile = "/dest/folder/file.txt";
        _fileSystemMock.Setup(fs => fs.DirectoryExists("/dest/folder")).Returns(true);
        var service = CreateService();

        var result = service.PrepareDestinationDirectory(destinationFile);

        Assert.False(result.Created);
        Assert.Equal("/dest/folder", result.DirectoryPath);
        _fileSystemMock.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void PrepareDestinationDirectory_WhenDirectoryMissing_CreatesDirectory()
    {
        const string destinationFile = "/dest/folder/file.txt";
        _fileSystemMock.Setup(fs => fs.DirectoryExists("/dest/folder")).Returns(false);
        var service = CreateService();

        var result = service.PrepareDestinationDirectory(destinationFile);

        Assert.True(result.Created);
        Assert.Equal("/dest/folder", result.DirectoryPath);
        _fileSystemMock.Verify(fs => fs.CreateDirectory("/dest/folder"), Times.Once);
    }

    [Fact]
    public void TransferAndEncrypt_WhenTransferFails_ThrowsTranslatedException()
    {
        var plan = new BackupFilePlan("/src/a.txt", "/dst/a.txt", 10, false, true);
        _transferServiceMock
            .Setup(ts => ts.TransferFile(plan.SourceFile, plan.DestinationFile, true))
            .Returns(new TransferResult(0, 0, 42));
        var service = CreateService();

        var ex = Assert.Throws<EasysaveDefaultException>(() => service.TransferAndEncrypt(plan, EncryptionPolicy.Disabled));

        Assert.Equal(LocalizationKey.error_file_transfer_failed, ex.ErrorKey);
        Assert.Equal(3, ex.Options.Count);
        Assert.Equal(plan.SourceFile, ex.Options[0]);
        Assert.Equal(plan.DestinationFile, ex.Options[1]);
        Assert.Equal("42", ex.Options[2]);
    }

    [Fact]
    public void TransferAndEncrypt_WhenFileNotTargetedForEncryption_DoesNotResolveProvider()
    {
        var plan = new BackupFilePlan("/src/a.png", "/dst/a.png", 120, false, true);
        _transferServiceMock
            .Setup(ts => ts.TransferFile(plan.SourceFile, plan.DestinationFile, true))
            .Returns(new TransferResult(120, 10, 0));
        var service = CreateService();
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);

        var result = service.TransferAndEncrypt(plan, policy);

        Assert.Equal(0, result.EncryptionTimeMs);
        _resolverMock.Verify(r => r.Resolve(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void TransferAndEncrypt_WhenProviderNotFound_ReturnsNegativeEncryptionTime()
    {
        var plan = new BackupFilePlan("/src/a.txt", "/dst/a.txt", 120, false, true);
        _transferServiceMock
            .Setup(ts => ts.TransferFile(plan.SourceFile, plan.DestinationFile, true))
            .Returns(new TransferResult(120, 10, 0));
        _resolverMock.Setup(r => r.Resolve(EncryptionProviderNames.DotNet)).Returns((IEncryptionProvider?)null);
        var service = CreateService();
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);

        var result = service.TransferAndEncrypt(plan, policy);

        Assert.Equal(-1, result.EncryptionTimeMs);
    }

    [Fact]
    public void TransferAndEncrypt_WhenProviderSucceeds_ReturnsEncryptionTime()
    {
        var plan = new BackupFilePlan("/src/a.txt", "/dst/a.txt", 120, false, true);
        _transferServiceMock
            .Setup(ts => ts.TransferFile(plan.SourceFile, plan.DestinationFile, true))
            .Returns(new TransferResult(120, 10, 0));
        _resolverMock.Setup(r => r.Resolve(EncryptionProviderNames.DotNet)).Returns(_providerMock.Object);
        _providerMock
            .Setup(p => p.EncryptAsync(plan.DestinationFile, It.IsAny<EncryptionPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EncryptionResult.Success(17));
        var service = CreateService();
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);

        var result = service.TransferAndEncrypt(plan, policy);

        Assert.Equal(17, result.EncryptionTimeMs);
    }

    [Fact]
    public void TransferAndEncrypt_WhenProviderFailsWithPositiveTime_ReturnsNegativeTime()
    {
        var plan = new BackupFilePlan("/src/a.txt", "/dst/a.txt", 120, false, true);
        _transferServiceMock
            .Setup(ts => ts.TransferFile(plan.SourceFile, plan.DestinationFile, true))
            .Returns(new TransferResult(120, 10, 0));
        _resolverMock.Setup(r => r.Resolve(EncryptionProviderNames.DotNet)).Returns(_providerMock.Object);
        _providerMock
            .Setup(p => p.EncryptAsync(plan.DestinationFile, It.IsAny<EncryptionPolicy>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EncryptionResult(IsSuccess: false, EncryptionTimeMs: 9, ErrorCode: 123, ErrorMessage: "boom"));
        var service = CreateService();
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);

        var result = service.TransferAndEncrypt(plan, policy);

        Assert.Equal(-9, result.EncryptionTimeMs);
    }

    [Fact]
    public void TransferAndEncrypt_WhenProviderThrows_ReturnsNegativeOne()
    {
        var plan = new BackupFilePlan("/src/a.txt", "/dst/a.txt", 120, false, true);
        _transferServiceMock
            .Setup(ts => ts.TransferFile(plan.SourceFile, plan.DestinationFile, true))
            .Returns(new TransferResult(120, 10, 0));
        _resolverMock.Setup(r => r.Resolve(EncryptionProviderNames.DotNet)).Returns(_providerMock.Object);
        _providerMock
            .Setup(p => p.EncryptAsync(plan.DestinationFile, It.IsAny<EncryptionPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("cryptofail"));
        var service = CreateService();
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);

        var result = service.TransferAndEncrypt(plan, policy);

        Assert.Equal(-1, result.EncryptionTimeMs);
    }

    [Fact]
    public void TransferAndEncrypt_WhenProviderIsCanceled_PropagatesOperationCanceledException()
    {
        var plan = new BackupFilePlan("/src/a.txt", "/dst/a.txt", 120, false, true);
        _transferServiceMock
            .Setup(ts => ts.TransferFile(plan.SourceFile, plan.DestinationFile, true))
            .Returns(new TransferResult(120, 10, 0));
        _resolverMock.Setup(r => r.Resolve(EncryptionProviderNames.DotNet)).Returns(_providerMock.Object);

        using var cts = new CancellationTokenSource();
        _providerMock
            .Setup(p => p.EncryptAsync(plan.DestinationFile, It.IsAny<EncryptionPolicy>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var service = CreateService();
        var policy = new EncryptionPolicy([".txt"], EncryptionProviderNames.DotNet, null);

        Assert.Throws<OperationCanceledException>(() => service.TransferAndEncrypt(plan, policy, cts.Token));
    }

    private BackupFileExecutionService CreateService()
        => new(_fileSystemMock.Object, _transferServiceMock.Object, _resolverMock.Object);
}
