namespace EasySave.System.Tests;

public class TransferResultTests
{
    #region IsSuccess Tests

    [Fact]
    public void IsSuccess_ErrorCodeZero_ReturnsTrue()
    {
        // Arrange
        var result = new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 50, ErrorCode: 0);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_ErrorCodeNonZero_ReturnsFalse()
    {
        // Arrange
        var result = new TransferResult(FileSizeBytes: 0, TransferTimeMs: -1, ErrorCode: -1);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_ErrorCodePositive_ReturnsFalse()
    {
        // Arrange
        var result = new TransferResult(FileSizeBytes: 0, TransferTimeMs: -1, ErrorCode: 5);

        // Assert
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Factory Methods Tests

    [Fact]
    public void InvalidSourcePath_ReturnsCorrectErrorCode()
    {
        // Act
        var result = TransferResult.InvalidSourcePath();

        // Assert
        Assert.Equal(TransferResult.ErrorCodes.InvalidSourcePath, result.ErrorCode);
    }

    [Fact]
    public void InvalidSourcePath_ReturnsZeroFileSize()
    {
        // Act
        var result = TransferResult.InvalidSourcePath();

        // Assert
        Assert.Equal(0, result.FileSizeBytes);
    }

    [Fact]
    public void InvalidSourcePath_ReturnsNegativeTransferTime()
    {
        // Act
        var result = TransferResult.InvalidSourcePath();

        // Assert
        Assert.Equal(-1, result.TransferTimeMs);
    }

    [Fact]
    public void InvalidSourcePath_IsNotSuccess()
    {
        // Act
        var result = TransferResult.InvalidSourcePath();

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void InvalidDestinationPath_ReturnsCorrectErrorCode()
    {
        // Act
        var result = TransferResult.InvalidDestinationPath();

        // Assert
        Assert.Equal(TransferResult.ErrorCodes.InvalidDestinationPath, result.ErrorCode);
    }

    [Fact]
    public void InvalidDestinationPath_ReturnsZeroFileSize()
    {
        // Act
        var result = TransferResult.InvalidDestinationPath();

        // Assert
        Assert.Equal(0, result.FileSizeBytes);
    }

    [Fact]
    public void InvalidDestinationPath_ReturnsNegativeTransferTime()
    {
        // Act
        var result = TransferResult.InvalidDestinationPath();

        // Assert
        Assert.Equal(-1, result.TransferTimeMs);
    }

    [Fact]
    public void InvalidDestinationPath_IsNotSuccess()
    {
        // Act
        var result = TransferResult.InvalidDestinationPath();

        // Assert
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region ErrorCodes Constants Tests

    [Fact]
    public void ErrorCodes_None_IsZero()
    {
        Assert.Equal(0, TransferResult.ErrorCodes.None);
    }

    [Fact]
    public void ErrorCodes_InvalidSourcePath_IsNegative()
    {
        Assert.Equal(-1, TransferResult.ErrorCodes.InvalidSourcePath);
    }

    [Fact]
    public void ErrorCodes_InvalidDestinationPath_IsNegative()
    {
        Assert.Equal(-2, TransferResult.ErrorCodes.InvalidDestinationPath);
    }

    [Fact]
    public void ErrorCodes_SourceNotFound_IsNegative()
    {
        Assert.Equal(-3, TransferResult.ErrorCodes.SourceNotFound);
    }

    [Fact]
    public void ErrorCodes_AllAreUnique()
    {
        var codes = new[]
        {
            TransferResult.ErrorCodes.None,
            TransferResult.ErrorCodes.InvalidSourcePath,
            TransferResult.ErrorCodes.InvalidDestinationPath,
            TransferResult.ErrorCodes.SourceNotFound
        };

        Assert.Equal(codes.Length, codes.Distinct().Count());
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var result1 = new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 50, ErrorCode: 0);
        var result2 = new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 50, ErrorCode: 0);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var result1 = new TransferResult(FileSizeBytes: 1000, TransferTimeMs: 50, ErrorCode: 0);
        var result2 = new TransferResult(FileSizeBytes: 2000, TransferTimeMs: 50, ErrorCode: 0);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    #endregion
}
