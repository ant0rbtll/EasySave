using EasySave.Configuration;
using Moq;

namespace EasySave.Persistence.Tests;

public class JsonUserPreferencesRepositoryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testFilePath;
    private readonly Mock<IPathProvider> _pathProviderMock;

    public JsonUserPreferencesRepositoryTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"EasySaveTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _testFilePath = Path.Combine(_testDirectory, "user-preferences.json");

        _pathProviderMock = new Mock<IPathProvider>();
        _pathProviderMock.Setup(p => p.GetUserPreferencesPath()).Returns(_testFilePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        GC.SuppressFinalize(this);
    }

    private JsonUserPreferencesRepository CreateRepository()
    {
        return new JsonUserPreferencesRepository(_pathProviderMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullPathProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new JsonUserPreferencesRepository(null!));
    }

    [Fact]
    public void Constructor_WithValidPathProvider_CreatesInstance()
    {
        // Act
        var repo = CreateRepository();

        // Assert
        Assert.NotNull(repo);
    }

    #endregion

    #region Load Tests

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultPreferences()
    {
        // Arrange
        var repo = CreateRepository();

        // Act
        var preferences = repo.Load();

        // Assert
        Assert.NotNull(preferences);
        Assert.Equal("fr", preferences.Language);
        _pathProviderMock.Verify(p => p.GetUserPreferencesPath(), Times.Once);
    }

    [Fact]
    public void Load_WhenFileIsEmpty_ReturnsDefaultPreferences()
    {
        // Arrange
        File.WriteAllText(_testFilePath, string.Empty);
        var repo = CreateRepository();

        // Act
        var preferences = repo.Load();

        // Assert
        Assert.NotNull(preferences);
        Assert.Equal("fr", preferences.Language);
    }

    [Fact]
    public void Load_WhenFileContainsWhitespaceOnly_ReturnsDefaultPreferences()
    {
        // Arrange
        File.WriteAllText(_testFilePath, "   \n\t  ");
        var repo = CreateRepository();

        // Act
        var preferences = repo.Load();

        // Assert
        Assert.NotNull(preferences);
        Assert.Equal("fr", preferences.Language);
    }

    [Fact]
    public void Load_WithCorruptedJson_ReturnsDefaultPreferences()
    {
        // Arrange
        File.WriteAllText(_testFilePath, "{ invalid json content ]}");
        var repo = CreateRepository();

        // Act
        var preferences = repo.Load();

        // Assert
        Assert.NotNull(preferences);
        Assert.Equal("fr", preferences.Language);
    }

    [Fact]
    public void Load_WithValidJson_ReturnsDeserializedPreferences()
    {
        // Arrange
        File.WriteAllText(_testFilePath, "{\"language\": \"en\"}");
        var repo = CreateRepository();

        // Act
        var preferences = repo.Load();

        // Assert
        Assert.NotNull(preferences);
        Assert.Equal("en", preferences.Language);
    }

    [Fact]
    public void Load_WithNullDeserializationResult_ReturnsDefaultPreferences()
    {
        // Arrange
        File.WriteAllText(_testFilePath, "null");
        var repo = CreateRepository();

        // Act
        var preferences = repo.Load();

        // Assert
        Assert.NotNull(preferences);
        Assert.Equal("fr", preferences.Language);
    }

    #endregion

    #region Save Tests

    [Fact]
    public void Save_WithNullPreferences_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateRepository();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => repo.Save(null!));
    }

    [Fact]
    public void Save_CreatesFileAndPersistsData()
    {
        // Arrange
        var repo = CreateRepository();
        var preferences = new UserPreferences { Language = "en" };

        // Act
        repo.Save(preferences);

        // Assert
        Assert.True(File.Exists(_testFilePath));
        var content = File.ReadAllText(_testFilePath);
        Assert.Contains("\"language\"", content);
        Assert.Contains("\"en\"", content);
        _pathProviderMock.Verify(p => p.GetUserPreferencesPath(), Times.Once);
    }

    [Fact]
    public void Save_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var nestedPath = Path.Combine(_testDirectory, "nested", "folder", "prefs.json");
        _pathProviderMock.Setup(p => p.GetUserPreferencesPath()).Returns(nestedPath);
        var repo = CreateRepository();
        var preferences = new UserPreferences { Language = "de" };

        // Act
        repo.Save(preferences);

        // Assert
        Assert.True(File.Exists(nestedPath));
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        // Arrange
        var repo = CreateRepository();
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

    [Fact]
    public void SaveAndLoad_PersistsLogDirectory()
    {
        // Arrange
        var repo = new JsonUserPreferencesRepository(_pathProvider);
        var preferences = new UserPreferences
        {
            Language = "en",
            LogDirectory = "/tmp/easysave-logs"
        };

        // Act
        repo.Save(preferences);
        var loadedPreferences = repo.Load();

        // Assert
        Assert.NotNull(loadedPreferences);
        Assert.Equal("/tmp/easysave-logs", loadedPreferences.LogDirectory);
    }

    [Fact]
    public void Load_WhenLogDirectoryIsMissing_ReturnsNull()
    {
        // Arrange
        var repo = new JsonUserPreferencesRepository(_pathProvider);
        File.WriteAllText(_testFilePath, "{ \"language\": \"en\" }");

        // Act
        var loadedPreferences = repo.Load();

        // Assert
        Assert.NotNull(loadedPreferences);
        Assert.Null(loadedPreferences.LogDirectory);
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
        public void SetLogDirectoryOverride(string? directory) { }
    }
    [Fact]
    public void Save_UsesCorrectJsonFormat()
    {
        // Arrange
        var repo = CreateRepository();
        var preferences = new UserPreferences { Language = "it" };

        // Act
        repo.Save(preferences);

        // Assert
        var content = File.ReadAllText(_testFilePath);
        Assert.Contains("\"language\"", content); // camelCase
        Assert.DoesNotContain("\"Language\"", content); // Not PascalCase
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Load_AfterSave_ReturnsPersistedData()
    {
        // Arrange
        var repo = CreateRepository();
        var originalPreferences = new UserPreferences { Language = "en" };

        // Act
        repo.Save(originalPreferences);
        var loadedPreferences = repo.Load();

        // Assert
        Assert.NotNull(loadedPreferences);
        Assert.Equal("en", loadedPreferences.Language);
    }

    [Fact]
    public void Load_AfterSave_WithNewRepositoryInstance_ReturnsPersistedData()
    {
        // Arrange
        var repo1 = CreateRepository();
        var originalPreferences = new UserPreferences { Language = "pt" };
        repo1.Save(originalPreferences);

        // Act - New instance to verify persistence
        var repo2 = CreateRepository();
        var loadedPreferences = repo2.Load();

        // Assert
        Assert.NotNull(loadedPreferences);
        Assert.Equal("pt", loadedPreferences.Language);
    }

    [Fact]
    public void PathProvider_GetUserPreferencesPath_IsCalledOnEachOperation()
    {
        // Arrange
        var repo = CreateRepository();
        var preferences = new UserPreferences { Language = "fr" };

        // Act
        repo.Save(preferences);
        repo.Load();
        repo.Load();

        // Assert
        _pathProviderMock.Verify(p => p.GetUserPreferencesPath(), Times.Exactly(3));
    }

    #endregion
}
