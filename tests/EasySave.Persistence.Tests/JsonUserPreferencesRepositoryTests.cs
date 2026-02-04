using EasySave.Configuration;
using EasySave.Persistence;

namespace EasySave.Persistence.Tests;

public class JsonUserPreferencesRepositoryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testFilePath;
    private readonly TestPathProvider _pathProvider;

    public JsonUserPreferencesRepositoryTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"EasySaveTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _testFilePath = Path.Combine(_testDirectory, "user-preferences.json");
        _pathProvider = new TestPathProvider(_testFilePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultPreferences()
    {
        // Arrange
        var repo = new JsonUserPreferencesRepository(_pathProvider);

        // Act
        var preferences = repo.Load();

        // Assert
        Assert.NotNull(preferences);
        Assert.Equal("fr", preferences.Language);
    }

    [Fact]
    public void Load_WhenFileIsEmpty_ReturnsDefaultPreferences()
    {
        // Arrange
        File.WriteAllText(_testFilePath, string.Empty);
        var repo = new JsonUserPreferencesRepository(_pathProvider);

        // Act
        var preferences = repo.Load();

        // Assert
        Assert.NotNull(preferences);
        Assert.Equal("fr", preferences.Language);
    }

    [Fact]
    public void Save_CreatesFileAndPersistsData()
    {
        // Arrange
        var repo = new JsonUserPreferencesRepository(_pathProvider);
        var preferences = new UserPreferences { Language = "en" };

        // Act
        repo.Save(preferences);

        // Assert
        Assert.True(File.Exists(_testFilePath));
        var content = File.ReadAllText(_testFilePath);
        Assert.Contains("\"language\"", content);
        Assert.Contains("\"en\"", content);
    }

    [Fact]
    public void Load_AfterSave_ReturnsPersistedData()
    {
        // Arrange
        var repo = new JsonUserPreferencesRepository(_pathProvider);
        var originalPreferences = new UserPreferences { Language = "en" };

        // Act
        repo.Save(originalPreferences);
        var loadedPreferences = repo.Load();

        // Assert
        Assert.NotNull(loadedPreferences);
        Assert.Equal("en", loadedPreferences.Language);
    }

    [Fact]
    public void Load_WithCorruptedJson_ReturnsDefaultPreferences()
    {
        // Arrange
        File.WriteAllText(_testFilePath, "{ invalid json content ]}");
        var repo = new JsonUserPreferencesRepository(_pathProvider);

        // Act
        var preferences = repo.Load();

        // Assert
        Assert.NotNull(preferences);
        Assert.Equal("fr", preferences.Language);
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        // Arrange
        var repo = new JsonUserPreferencesRepository(_pathProvider);
        var firstPreferences = new UserPreferences { Language = "en" };
        var secondPreferences = new UserPreferences { Language = "es" };

        // Act
        repo.Save(firstPreferences);
        repo.Save(secondPreferences);
        var loadedPreferences = repo.Load();

        // Assert
        Assert.NotNull(loadedPreferences);
        Assert.Equal("es", loadedPreferences.Language);
    }

    // Helper class for tests
    private class TestPathProvider : IPathProvider
    {
        private readonly string _preferencesPath;

        public TestPathProvider(string preferencesPath)
        {
            _preferencesPath = preferencesPath;
        }

        public string GetDailyLogPath(DateTime date) => throw new NotImplementedException();
        public string GetStatePath() => throw new NotImplementedException();
        public string GetJobsConfigPath() => throw new NotImplementedException();
        public string GetUserPreferencesPath() => _preferencesPath;
    }
}
