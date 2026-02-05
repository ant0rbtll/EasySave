using Moq;

namespace EasySave.System.Tests;

public class DefaultTransferServiceTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;

    public DefaultTransferServiceTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
    }

    private DefaultTransferService CreateService()
    {
        return new DefaultTransferService(_fileSystemMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DefaultTransferService(null!));
    }

    [Fact]
    public void Constructor_ValidFileSystem_CreatesInstance()
    {
        // Act
        var service = CreateService();

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region TransferFile - Invalid Arguments Tests

    [Fact]
    public void TransferFile_NullSourcePath_ReturnsInvalidSourcePath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.TransferFile(null!, "/dest/file.txt", true);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransferResult.ErrorCodes.InvalidSourcePath, result.ErrorCode);
    }

    [Fact]
    public void TransferFile_EmptySourcePath_ReturnsInvalidSourcePath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.TransferFile("", "/dest/file.txt", true);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransferResult.ErrorCodes.InvalidSourcePath, result.ErrorCode);
    }

    [Fact]
    public void TransferFile_WhitespaceSourcePath_ReturnsInvalidSourcePath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.TransferFile("   ", "/dest/file.txt", true);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransferResult.ErrorCodes.InvalidSourcePath, result.ErrorCode);
    }

    [Fact]
    public void TransferFile_NullDestinationPath_ReturnsInvalidDestinationPath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.TransferFile("/source/file.txt", null!, true);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransferResult.ErrorCodes.InvalidDestinationPath, result.ErrorCode);
    }

    [Fact]
    public void TransferFile_EmptyDestinationPath_ReturnsInvalidDestinationPath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.TransferFile("/source/file.txt", "", true);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransferResult.ErrorCodes.InvalidDestinationPath, result.ErrorCode);
    }

    [Fact]
    public void TransferFile_WhitespaceDestinationPath_ReturnsInvalidDestinationPath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.TransferFile("/source/file.txt", "   ", true);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransferResult.ErrorCodes.InvalidDestinationPath, result.ErrorCode);
    }

    #endregion

    #region TransferFile - Source Not Found Tests

    [Fact]
    public void TransferFile_SourceNotFound_ReturnsSourceNotFoundError()
    {
        // Arrange
        var service = CreateService();
        _fileSystemMock.Setup(fs => fs.FileExists("/source/file.txt")).Returns(false);

        // Act
        var result = service.TransferFile("/source/file.txt", "/dest/file.txt", true);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(TransferResult.ErrorCodes.SourceNotFound, result.ErrorCode);
    }

    [Fact]
    public void TransferFile_SourceNotFound_DoesNotCallCopyFile()
    {
        // Arrange
        var service = CreateService();
        _fileSystemMock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        service.TransferFile("/source/file.txt", "/dest/file.txt", true);

        // Assert
        _fileSystemMock.Verify(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    #endregion

    #region TransferFile - Success Tests

    [Fact]
    public void TransferFile_ValidPaths_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();
        _fileSystemMock.Setup(fs => fs.FileExists("/source/file.txt")).Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/file.txt")).Returns(1000);

        // Act
        var result = service.TransferFile("/source/file.txt", "/dest/file.txt", true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TransferResult.ErrorCodes.None, result.ErrorCode);
    }

    [Fact]
    public void TransferFile_ValidPaths_ReturnsCorrectFileSize()
    {
        // Arrange
        var service = CreateService();
        _fileSystemMock.Setup(fs => fs.FileExists("/source/file.txt")).Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/file.txt")).Returns(5000);

        // Act
        var result = service.TransferFile("/source/file.txt", "/dest/file.txt", true);

        // Assert
        Assert.Equal(5000, result.FileSizeBytes);
    }

    [Fact]
    public void TransferFile_ValidPaths_ReturnsNonNegativeTransferTime()
    {
        // Arrange
        var service = CreateService();
        _fileSystemMock.Setup(fs => fs.FileExists("/source/file.txt")).Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/file.txt")).Returns(1000);

        // Act
        var result = service.TransferFile("/source/file.txt", "/dest/file.txt", true);

        // Assert
        Assert.True(result.TransferTimeMs >= 0);
    }

    [Fact]
    public void TransferFile_ValidPaths_CallsEnsureDirectoryForFileExists()
    {
        // Arrange
        var service = CreateService();
        _fileSystemMock.Setup(fs => fs.FileExists("/source/file.txt")).Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/file.txt")).Returns(1000);

        // Act
        service.TransferFile("/source/file.txt", "/dest/file.txt", true);

        // Assert
        _fileSystemMock.Verify(fs => fs.EnsureDirectoryForFileExists("/dest/file.txt"), Times.Once);
    }

    [Fact]
    public void TransferFile_ValidPaths_CallsCopyFileWithCorrectParameters()
    {
        // Arrange
        var service = CreateService();
        _fileSystemMock.Setup(fs => fs.FileExists("/source/file.txt")).Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/file.txt")).Returns(1000);

        // Act
        service.TransferFile("/source/file.txt", "/dest/file.txt", true);

        // Assert
        _fileSystemMock.Verify(fs => fs.CopyFile("/source/file.txt", "/dest/file.txt", true), Times.Once);
    }

    [Fact]
    public void TransferFile_OverwriteFalse_PassesOverwriteParameterCorrectly()
    {
        // Arrange
        var service = CreateService();
        _fileSystemMock.Setup(fs => fs.FileExists("/source/file.txt")).Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/file.txt")).Returns(1000);

        // Act
        service.TransferFile("/source/file.txt", "/dest/file.txt", false);

        // Assert
        _fileSystemMock.Verify(fs => fs.CopyFile("/source/file.txt", "/dest/file.txt", false), Times.Once);
    }

    #endregion

    #region TransferFile - Exception Handling Tests

    [Fact]
    public void TransferFile_CopyFileThrowsException_ReturnsErrorResult()
    {
        // Arrange
        var service = CreateService();
        _fileSystemMock.Setup(fs => fs.FileExists("/source/file.txt")).Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/file.txt")).Returns(1000);
        _fileSystemMock.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Throws(new IOException("Disk full"));

        // Act
        var result = service.TransferFile("/source/file.txt", "/dest/file.txt", true);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEqual(TransferResult.ErrorCodes.None, result.ErrorCode);
    }

    [Fact]
    public void TransferFile_CopyFileThrowsException_ReturnsNegativeTransferTime()
    {
        // Arrange
        var service = CreateService();
        _fileSystemMock.Setup(fs => fs.FileExists("/source/file.txt")).Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/file.txt")).Returns(1000);
        _fileSystemMock.Setup(fs => fs.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Throws(new IOException("Disk full"));

        // Act
        var result = service.TransferFile("/source/file.txt", "/dest/file.txt", true);

        // Assert
        Assert.True(result.TransferTimeMs < 0);
    }

    [Fact]
    public void TransferFile_GetFileSizeThrowsException_ReturnsErrorResult()
    {
        // Arrange
        var service = CreateService();
        _fileSystemMock.Setup(fs => fs.FileExists("/source/file.txt")).Returns(true);
        _fileSystemMock.Setup(fs => fs.GetFileSize("/source/file.txt"))
            .Throws(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = service.TransferFile("/source/file.txt", "/dest/file.txt", true);

        // Assert
        Assert.False(result.IsSuccess);
    }

    #endregion
}
