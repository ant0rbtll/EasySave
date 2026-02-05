namespace EasySave.Configuration.Tests;

public class DefaultPathProviderTests : IDisposable
{
    private readonly DefaultPathProvider _provider;
    private readonly string _baseDirectory;
    private readonly List<string> _createdFiles;
    private readonly List<string> _createdDirectories;

    public DefaultPathProviderTests()
    {
        _provider = new DefaultPathProvider();
        _baseDirectory = AppContext.BaseDirectory;
        _createdFiles = [];
        _createdDirectories = [];
    }

    public void Dispose()
    {
        // Clean up created files
        foreach (var file in _createdFiles.Where(File.Exists))
        {
            File.Delete(file);
        }

        // Clean up created directories (only if empty)
        foreach (var dir in _createdDirectories.Where(d => Directory.Exists(d) && !Directory.EnumerateFileSystemEntries(d).Any()))
        {
            Directory.Delete(dir);
        }

        GC.SuppressFinalize(this);
    }

    #region GetDailyLogPath Tests

    [Fact]
    public void GetDailyLogPath_ReturnsPathInLogsDirectory()
    {
        // Arrange
        var date = DateTime.Now;

        // Act
        var result = _provider.GetDailyLogPath(date);
        TrackCreatedPath(result);

        // Assert
        Assert.Contains("logs", result);
        Assert.Contains(Path.Combine(_baseDirectory, "logs"), result);
    }

    [Fact]
    public void GetDailyLogPath_ReturnsPathWithCorrectDateFormat()
    {
        // Arrange
        var date = new DateTime(2024, 12, 25);

        // Act
        var result = _provider.GetDailyLogPath(date);
        TrackCreatedPath(result);

        // Assert
        Assert.EndsWith("2024-12-25.json", result);
    }

    [Fact]
    public void GetDailyLogPath_CreatesLogsDirectory()
    {
        // Arrange
        var date = DateTime.Now;
        var logsDir = Path.Combine(_baseDirectory, "logs");

        // Act
        var result = _provider.GetDailyLogPath(date);
        TrackCreatedPath(result);
        _createdDirectories.Add(logsDir);

        // Assert
        Assert.True(Directory.Exists(logsDir));
    }

    [Fact]
    public void GetDailyLogPath_CreatesFile()
    {
        // Arrange
        var date = DateTime.Now;

        // Act
        var result = _provider.GetDailyLogPath(date);
        TrackCreatedPath(result);

        // Assert
        Assert.True(File.Exists(result));
    }

    [Fact]
    public void GetDailyLogPath_DifferentDates_ReturnDifferentPaths()
    {
        // Arrange
        var date1 = new DateTime(2024, 1, 1);
        var date2 = new DateTime(2024, 1, 2);

        // Act
        var result1 = _provider.GetDailyLogPath(date1);
        var result2 = _provider.GetDailyLogPath(date2);
        TrackCreatedPath(result1);
        TrackCreatedPath(result2);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void GetDailyLogPath_SameDate_ReturnsSamePath()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);

        // Act
        var result1 = _provider.GetDailyLogPath(date);
        var result2 = _provider.GetDailyLogPath(date);
        TrackCreatedPath(result1);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void GetDailyLogPath_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var date = DateTime.Now;

        // Act & Assert - Should not throw when file already exists
        var result1 = _provider.GetDailyLogPath(date);
        var result2 = _provider.GetDailyLogPath(date);
        TrackCreatedPath(result1);

        Assert.Equal(result1, result2);
    }

    #endregion

    #region GetStatePath Tests

    [Fact]
    public void GetStatePath_ReturnsPathInBaseDirectory()
    {
        // Act
        var result = _provider.GetStatePath();
        TrackCreatedPath(result);

        // Assert
        Assert.StartsWith(_baseDirectory, result);
    }

    [Fact]
    public void GetStatePath_ReturnsStateJsonFile()
    {
        // Act
        var result = _provider.GetStatePath();
        TrackCreatedPath(result);

        // Assert
        Assert.EndsWith("state.json", result);
    }

    [Fact]
    public void GetStatePath_CreatesFile()
    {
        // Act
        var result = _provider.GetStatePath();
        TrackCreatedPath(result);

        // Assert
        Assert.True(File.Exists(result));
    }

    [Fact]
    public void GetStatePath_CalledMultipleTimes_ReturnsSamePath()
    {
        // Act
        var result1 = _provider.GetStatePath();
        var result2 = _provider.GetStatePath();
        TrackCreatedPath(result1);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void GetStatePath_ReturnsCorrectFullPath()
    {
        // Arrange
        var expectedPath = Path.Combine(_baseDirectory, "state.json");

        // Act
        var result = _provider.GetStatePath();
        TrackCreatedPath(result);

        // Assert
        Assert.Equal(expectedPath, result);
    }

    #endregion

    #region GetJobsConfigPath Tests

    [Fact]
    public void GetJobsConfigPath_ReturnsPathInBaseDirectory()
    {
        // Act
        var result = _provider.GetJobsConfigPath();
        TrackCreatedPath(result);

        // Assert
        Assert.StartsWith(_baseDirectory, result);
    }

    [Fact]
    public void GetJobsConfigPath_ReturnsJobsJsonFile()
    {
        // Act
        var result = _provider.GetJobsConfigPath();
        TrackCreatedPath(result);

        // Assert
        Assert.EndsWith("jobs.json", result);
    }

    [Fact]
    public void GetJobsConfigPath_CreatesFile()
    {
        // Act
        var result = _provider.GetJobsConfigPath();
        TrackCreatedPath(result);

        // Assert
        Assert.True(File.Exists(result));
    }

    [Fact]
    public void GetJobsConfigPath_CalledMultipleTimes_ReturnsSamePath()
    {
        // Act
        var result1 = _provider.GetJobsConfigPath();
        var result2 = _provider.GetJobsConfigPath();
        TrackCreatedPath(result1);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void GetJobsConfigPath_ReturnsCorrectFullPath()
    {
        // Arrange
        var expectedPath = Path.Combine(_baseDirectory, "jobs.json");

        // Act
        var result = _provider.GetJobsConfigPath();
        TrackCreatedPath(result);

        // Assert
        Assert.Equal(expectedPath, result);
    }

    #endregion

    #region GetUserPreferencesPath Tests

    [Fact]
    public void GetUserPreferencesPath_ReturnsPathInBaseDirectory()
    {
        // Act
        var result = _provider.GetUserPreferencesPath();
        TrackCreatedPath(result);

        // Assert
        Assert.StartsWith(_baseDirectory, result);
    }

    [Fact]
    public void GetUserPreferencesPath_ReturnsUserPreferencesJsonFile()
    {
        // Act
        var result = _provider.GetUserPreferencesPath();
        TrackCreatedPath(result);

        // Assert
        Assert.EndsWith("user-preferences.json", result);
    }

    [Fact]
    public void GetUserPreferencesPath_CreatesFile()
    {
        // Act
        var result = _provider.GetUserPreferencesPath();
        TrackCreatedPath(result);

        // Assert
        Assert.True(File.Exists(result));
    }

    [Fact]
    public void GetUserPreferencesPath_CalledMultipleTimes_ReturnsSamePath()
    {
        // Act
        var result1 = _provider.GetUserPreferencesPath();
        var result2 = _provider.GetUserPreferencesPath();
        TrackCreatedPath(result1);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void GetUserPreferencesPath_ReturnsCorrectFullPath()
    {
        // Arrange
        var expectedPath = Path.Combine(_baseDirectory, "user-preferences.json");

        // Act
        var result = _provider.GetUserPreferencesPath();
        TrackCreatedPath(result);

        // Assert
        Assert.Equal(expectedPath, result);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var provider = new DefaultPathProvider();

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_MultipleInstances_UseSameBaseDirectory()
    {
        // Arrange
        var provider1 = new DefaultPathProvider();
        var provider2 = new DefaultPathProvider();

        // Act
        var path1 = provider1.GetJobsConfigPath();
        var path2 = provider2.GetJobsConfigPath();
        TrackCreatedPath(path1);

        // Assert
        Assert.Equal(path1, path2);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AllPaths_AreInSameBaseDirectory()
    {
        // Act
        var statePath = _provider.GetStatePath();
        var jobsPath = _provider.GetJobsConfigPath();
        var prefsPath = _provider.GetUserPreferencesPath();
        var logPath = _provider.GetDailyLogPath(DateTime.Now);

        TrackCreatedPath(statePath);
        TrackCreatedPath(jobsPath);
        TrackCreatedPath(prefsPath);
        TrackCreatedPath(logPath);

        // Assert - All paths should start with the same base directory
        Assert.StartsWith(_baseDirectory, statePath);
        Assert.StartsWith(_baseDirectory, jobsPath);
        Assert.StartsWith(_baseDirectory, prefsPath);
        Assert.StartsWith(_baseDirectory, logPath);
    }

    [Fact]
    public void AllPaths_AreUnique()
    {
        // Act
        var statePath = _provider.GetStatePath();
        var jobsPath = _provider.GetJobsConfigPath();
        var prefsPath = _provider.GetUserPreferencesPath();
        var logPath = _provider.GetDailyLogPath(DateTime.Now);

        TrackCreatedPath(statePath);
        TrackCreatedPath(jobsPath);
        TrackCreatedPath(prefsPath);
        TrackCreatedPath(logPath);

        var paths = new[] { statePath, jobsPath, prefsPath, logPath };

        // Assert - All paths should be unique
        Assert.Equal(paths.Length, paths.Distinct().Count());
    }

    [Fact]
    public void CreatedFiles_AreEmptyByDefault()
    {
        // Act
        var statePath = _provider.GetStatePath();
        var jobsPath = _provider.GetJobsConfigPath();
        var prefsPath = _provider.GetUserPreferencesPath();

        TrackCreatedPath(statePath);
        TrackCreatedPath(jobsPath);
        TrackCreatedPath(prefsPath);

        // Assert - All created files should be empty
        Assert.Empty(File.ReadAllText(statePath));
        Assert.Empty(File.ReadAllText(jobsPath));
        Assert.Empty(File.ReadAllText(prefsPath));
    }

    #endregion

    #region Helper Methods

    private void TrackCreatedPath(string path)
    {
        if (!_createdFiles.Contains(path))
        {
            _createdFiles.Add(path);
        }

        var directory = Path.GetDirectoryName(path);
        if (directory != null && directory != _baseDirectory && !_createdDirectories.Contains(directory))
        {
            _createdDirectories.Add(directory);
        }
    }

    #endregion
}
