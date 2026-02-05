namespace EasySave.System.Tests;

public class DefaultFileSystemTests : IDisposable
{
    private readonly DefaultFileSystem _fileSystem;
    private readonly string _testDirectory;

    public DefaultFileSystemTests()
    {
        _fileSystem = new DefaultFileSystem();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"EasySaveFileSystemTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        GC.SuppressFinalize(this);
    }

    #region DirectoryExists Tests

    [Fact]
    public void DirectoryExists_ExistingDirectory_ReturnsTrue()
    {
        // Assert
        Assert.True(_fileSystem.DirectoryExists(_testDirectory));
    }

    [Fact]
    public void DirectoryExists_NonExistentDirectory_ReturnsFalse()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "nonexistent");

        // Assert
        Assert.False(_fileSystem.DirectoryExists(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DirectoryExists_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.DirectoryExists(path!));
    }

    #endregion

    #region CreateDirectory Tests

    [Fact]
    public void CreateDirectory_ValidPath_CreatesDirectory()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "newdir");

        // Act
        _fileSystem.CreateDirectory(path);

        // Assert
        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public void CreateDirectory_NestedPath_CreatesAllDirectories()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "level1", "level2", "level3");

        // Act
        _fileSystem.CreateDirectory(path);

        // Assert
        Assert.True(Directory.Exists(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateDirectory_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.CreateDirectory(path!));
    }

    #endregion

    #region FileExists Tests

    [Fact]
    public void FileExists_ExistingFile_ReturnsTrue()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(path, "content");

        // Assert
        Assert.True(_fileSystem.FileExists(path));
    }

    [Fact]
    public void FileExists_NonExistentFile_ReturnsFalse()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "nonexistent.txt");

        // Assert
        Assert.False(_fileSystem.FileExists(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FileExists_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.FileExists(path!));
    }

    #endregion

    #region GetFileSize Tests

    [Fact]
    public void GetFileSize_ExistingFile_ReturnsCorrectSize()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "test.txt");
        var content = "Hello, World!";
        File.WriteAllText(path, content);

        // Act
        var size = _fileSystem.GetFileSize(path);

        // Assert
        Assert.Equal(new FileInfo(path).Length, size);
    }

    [Fact]
    public void GetFileSize_EmptyFile_ReturnsZero()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "empty.txt");
        File.WriteAllText(path, "");

        // Act
        var size = _fileSystem.GetFileSize(path);

        // Assert
        Assert.Equal(0, size);
    }

    [Fact]
    public void GetFileSize_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _fileSystem.GetFileSize(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetFileSize_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.GetFileSize(path!));
    }

    #endregion

    #region CopyFile Tests

    [Fact]
    public void CopyFile_ValidPaths_CopiesFile()
    {
        // Arrange
        var sourcePath = Path.Combine(_testDirectory, "source.txt");
        var destPath = Path.Combine(_testDirectory, "dest.txt");
        File.WriteAllText(sourcePath, "content");

        // Act
        _fileSystem.CopyFile(sourcePath, destPath, true);

        // Assert
        Assert.True(File.Exists(destPath));
        Assert.Equal("content", File.ReadAllText(destPath));
    }

    [Fact]
    public void CopyFile_OverwriteTrue_OverwritesExistingFile()
    {
        // Arrange
        var sourcePath = Path.Combine(_testDirectory, "source.txt");
        var destPath = Path.Combine(_testDirectory, "dest.txt");
        File.WriteAllText(sourcePath, "new content");
        File.WriteAllText(destPath, "old content");

        // Act
        _fileSystem.CopyFile(sourcePath, destPath, true);

        // Assert
        Assert.Equal("new content", File.ReadAllText(destPath));
    }

    [Fact]
    public void CopyFile_OverwriteFalse_ThrowsWhenFileExists()
    {
        // Arrange
        var sourcePath = Path.Combine(_testDirectory, "source.txt");
        var destPath = Path.Combine(_testDirectory, "dest.txt");
        File.WriteAllText(sourcePath, "new content");
        File.WriteAllText(destPath, "old content");

        // Act & Assert
        Assert.Throws<IOException>(() => _fileSystem.CopyFile(sourcePath, destPath, false));
    }

    [Theory]
    [InlineData(null, "/dest")]
    [InlineData("", "/dest")]
    [InlineData("   ", "/dest")]
    public void CopyFile_InvalidSourcePath_ThrowsArgumentException(string? source, string dest)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.CopyFile(source!, dest, true));
    }

    [Theory]
    [InlineData("/source", null)]
    [InlineData("/source", "")]
    [InlineData("/source", "   ")]
    public void CopyFile_InvalidDestinationPath_ThrowsArgumentException(string source, string? dest)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.CopyFile(source, dest!, true));
    }

    #endregion

    #region EnsureDirectoryForFileExists Tests

    [Fact]
    public void EnsureDirectoryForFileExists_DirectoryNotExists_CreatesDirectory()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "newdir", "file.txt");

        // Act
        _fileSystem.EnsureDirectoryForFileExists(filePath);

        // Assert
        Assert.True(Directory.Exists(Path.GetDirectoryName(filePath)));
    }

    [Fact]
    public void EnsureDirectoryForFileExists_DirectoryExists_DoesNotThrow()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "file.txt");

        // Act & Assert
        var exception = Record.Exception(() => _fileSystem.EnsureDirectoryForFileExists(filePath));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureDirectoryForFileExists_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.EnsureDirectoryForFileExists(path!));
    }

    #endregion

    #region EnumerateFilesRecursive Tests

    [Fact]
    public void EnumerateFilesRecursive_WithFiles_ReturnsAllFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "file1.txt"), "");
        Directory.CreateDirectory(Path.Combine(_testDirectory, "subdir"));
        File.WriteAllText(Path.Combine(_testDirectory, "subdir", "file2.txt"), "");

        // Act
        var files = _fileSystem.EnumerateFilesRecursive(_testDirectory).ToList();

        // Assert
        Assert.Equal(2, files.Count);
    }

    [Fact]
    public void EnumerateFilesRecursive_EmptyDirectory_ReturnsEmpty()
    {
        // Arrange
        var emptyDir = Path.Combine(_testDirectory, "empty");
        Directory.CreateDirectory(emptyDir);

        // Act
        var files = _fileSystem.EnumerateFilesRecursive(emptyDir).ToList();

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public void EnumerateFilesRecursive_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "nonexistent");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => _fileSystem.EnumerateFilesRecursive(path).ToList());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnumerateFilesRecursive_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.EnumerateFilesRecursive(path!).ToList());
    }

    #endregion

    #region EnumerateDirectoriesRecursive Tests

    [Fact]
    public void EnumerateDirectoriesRecursive_WithDirectories_ReturnsAllDirectories()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testDirectory, "dir1"));
        Directory.CreateDirectory(Path.Combine(_testDirectory, "dir1", "subdir"));

        // Act
        var dirs = _fileSystem.EnumerateDirectoriesRecursive(_testDirectory).ToList();

        // Assert
        Assert.Equal(2, dirs.Count);
    }

    [Fact]
    public void EnumerateDirectoriesRecursive_NoSubdirectories_ReturnsEmpty()
    {
        // Act
        var dirs = _fileSystem.EnumerateDirectoriesRecursive(_testDirectory).ToList();

        // Assert
        Assert.Empty(dirs);
    }

    [Fact]
    public void EnumerateDirectoriesRecursive_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var path = Path.Combine(_testDirectory, "nonexistent");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => _fileSystem.EnumerateDirectoriesRecursive(path).ToList());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnumerateDirectoriesRecursive_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.EnumerateDirectoriesRecursive(path!).ToList());
    }

    #endregion

    #region Combine Tests

    [Fact]
    public void Combine_ValidParts_ReturnsCombinedPath()
    {
        // Act
        var result = _fileSystem.Combine("path", "to", "file.txt");

        // Assert
        Assert.Equal(Path.Combine("path", "to", "file.txt"), result);
    }

    [Fact]
    public void Combine_NullParts_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _fileSystem.Combine(null!));
    }

    [Fact]
    public void Combine_EmptyParts_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.Combine(Array.Empty<string>()));
    }

    [Fact]
    public void Combine_PartsWithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.Combine("path", "   ", "file.txt"));
    }

    #endregion

    #region NormalizePath Tests

    [Fact]
    public void NormalizePath_ValidPath_ReturnsNormalizedPath()
    {
        // Act
        var result = _fileSystem.NormalizePath("path/to/file.txt");

        // Assert
        Assert.Equal($"path{Path.DirectorySeparatorChar}to{Path.DirectorySeparatorChar}file.txt", result);
    }

    [Fact]
    public void NormalizePath_PathWithBackslashes_NormalizesToSeparator()
    {
        // Act
        var result = _fileSystem.NormalizePath("path\\to\\file.txt");

        // Assert
        Assert.Equal($"path{Path.DirectorySeparatorChar}to{Path.DirectorySeparatorChar}file.txt", result);
    }

    [Fact]
    public void NormalizePath_PathWithLeadingTrailingSpaces_TrimsSpaces()
    {
        // Act
        var result = _fileSystem.NormalizePath("  path/to/file.txt  ");

        // Assert
        Assert.Equal($"path{Path.DirectorySeparatorChar}to{Path.DirectorySeparatorChar}file.txt", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizePath_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.NormalizePath(path!));
    }

    #endregion

    #region GetRelativePath Tests

    [Fact]
    public void GetRelativePath_ValidPaths_ReturnsRelativePath()
    {
        // Arrange
        var rootPath = "/root/path";
        var fullPath = "/root/path/sub/file.txt";

        // Act
        var result = _fileSystem.GetRelativePath(rootPath, fullPath);

        // Assert
        Assert.Equal(Path.GetRelativePath(rootPath, fullPath), result);
    }

    [Theory]
    [InlineData(null, "/full/path")]
    [InlineData("", "/full/path")]
    [InlineData("   ", "/full/path")]
    public void GetRelativePath_InvalidRootPath_ThrowsArgumentException(string? rootPath, string fullPath)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.GetRelativePath(rootPath!, fullPath));
    }

    [Theory]
    [InlineData("/root/path", null)]
    [InlineData("/root/path", "")]
    [InlineData("/root/path", "   ")]
    public void GetRelativePath_InvalidFullPath_ThrowsArgumentException(string rootPath, string? fullPath)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _fileSystem.GetRelativePath(rootPath, fullPath!));
    }

    #endregion
}
