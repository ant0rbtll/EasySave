using EasySave.Configuration;
using Moq;

namespace EasySave.State.Tests;

public class RealTimeStateWriterTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testFilePath;
    private readonly Mock<IPathProvider> _pathProviderMock;
    private readonly GlobalState _state;

    public RealTimeStateWriterTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"EasySaveStateTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _testFilePath = Path.Combine(_testDirectory, "state.json");

        _pathProviderMock = new Mock<IPathProvider>();
        _pathProviderMock.Setup(p => p.GetStatePath()).Returns(_testFilePath);

        _state = new GlobalState { Entries = new Dictionary<int, StateEntry>() };
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        GC.SuppressFinalize(this);
    }

    private RealTimeStateWriter CreateWriter()
    {
        return new RealTimeStateWriter(_pathProviderMock.Object, _state);
    }

    #region Update Tests

    [Fact]
    public void Update_ValidEntry_AddsEntryToState()
    {
        // Arrange
        var writer = CreateWriter();
        var entry = new StateEntry
        {
            BackupId = 1,
            BackupName = "TestBackup",
            Status = BackupStatus.Active
        };

        // Act
        writer.Update(entry);

        // Assert
        Assert.True(_state.Entries.TryGetValue(1, out var storedEntry));
        Assert.Equal("TestBackup", storedEntry.BackupName);
    }

    [Fact]
    public void Update_ValidEntry_WritesFileToPath()
    {
        // Arrange
        var writer = CreateWriter();
        var entry = new StateEntry
        {
            BackupId = 1,
            BackupName = "TestBackup",
            Status = BackupStatus.Active
        };

        // Act
        writer.Update(entry);

        // Assert
        Assert.True(File.Exists(_testFilePath));
        _pathProviderMock.Verify(p => p.GetStatePath(), Times.Once);
    }

    [Fact]
    public void Update_ValidEntry_WritesValidJson()
    {
        // Arrange
        var writer = CreateWriter();
        var entry = new StateEntry
        {
            BackupId = 1,
            BackupName = "TestBackup",
            Status = BackupStatus.Active
        };

        // Act
        writer.Update(entry);

        // Assert
        var json = File.ReadAllText(_testFilePath);
        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("TestBackup", json);
    }

    [Fact]
    public void Update_NullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var writer = CreateWriter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => writer.Update(null!));
    }

    [Fact]
    public void Update_MultipleEntries_AddsAllToState()
    {
        // Arrange
        var writer = CreateWriter();
        var entry1 = new StateEntry { BackupId = 1, BackupName = "Backup1" };
        var entry2 = new StateEntry { BackupId = 2, BackupName = "Backup2" };

        // Act
        writer.Update(entry1);
        writer.Update(entry2);

        // Assert
        Assert.Equal(2, _state.Entries.Count);
        Assert.True(_state.Entries.ContainsKey(1));
        Assert.True(_state.Entries.ContainsKey(2));
    }

    [Fact]
    public void Update_SameIdTwice_OverwritesEntry()
    {
        // Arrange
        var writer = CreateWriter();
        var entry1 = new StateEntry { BackupId = 1, BackupName = "Original" };
        var entry2 = new StateEntry { BackupId = 1, BackupName = "Updated" };

        // Act
        writer.Update(entry1);
        writer.Update(entry2);

        // Assert
        Assert.Single(_state.Entries);
        Assert.Equal("Updated", _state.Entries[1].BackupName);
    }

    [Fact]
    public void Update_SetsGlobalStateUpdatedAt()
    {
        // Arrange
        var writer = CreateWriter();
        var beforeUpdate = DateTime.Now;
        var entry = new StateEntry { BackupId = 1, BackupName = "Test" };

        // Act
        writer.Update(entry);

        // Assert
        Assert.True(_state.UpdatedAt >= beforeUpdate);
    }

    #endregion

    #region MarkInactive Tests

    [Fact]
    public void MarkInactive_ExistingEntry_SetsStatusToInactive()
    {
        // Arrange
        var entry = new StateEntry
        {
            BackupId = 1,
            BackupName = "Test",
            Status = BackupStatus.Active
        };
        _state.Entries[1] = entry;
        var writer = CreateWriter();

        // Act
        writer.MarkInactive(1);

        // Assert
        Assert.Equal(BackupStatus.Inactive, _state.Entries[1].Status);
    }

    [Fact]
    public void MarkInactive_ExistingEntry_UpdatesTimestamp()
    {
        // Arrange
        var entry = new StateEntry
        {
            BackupId = 1,
            BackupName = "Test",
            Status = BackupStatus.Active,
            Timestamp = DateTime.MinValue
        };
        _state.Entries[1] = entry;
        var writer = CreateWriter();
        var beforeMark = DateTime.Now;

        // Act
        writer.MarkInactive(1);

        // Assert
        Assert.True(_state.Entries[1].Timestamp >= beforeMark);
    }

    [Fact]
    public void MarkInactive_ExistingEntry_WritesFile()
    {
        // Arrange
        var entry = new StateEntry { BackupId = 1, BackupName = "Test" };
        _state.Entries[1] = entry;
        var writer = CreateWriter();

        // Act
        writer.MarkInactive(1);

        // Assert
        Assert.True(File.Exists(_testFilePath));
        _pathProviderMock.Verify(p => p.GetStatePath(), Times.Once);
    }

    [Fact]
    public void MarkInactive_NonExistentEntry_DoesNotThrow()
    {
        // Arrange
        var writer = CreateWriter();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => writer.MarkInactive(999));
        Assert.Null(exception);
    }

    [Fact]
    public void MarkInactive_NonExistentEntry_DoesNotWriteFile()
    {
        // Arrange
        var writer = CreateWriter();

        // Act
        writer.MarkInactive(999);

        // Assert - File should not be created since entry doesn't exist
        Assert.False(File.Exists(_testFilePath));
        _pathProviderMock.Verify(p => p.GetStatePath(), Times.Never);
    }

    [Fact]
    public void MarkInactive_SetsGlobalStateUpdatedAt()
    {
        // Arrange
        var entry = new StateEntry { BackupId = 1, BackupName = "Test" };
        _state.Entries[1] = entry;
        var writer = CreateWriter();
        var beforeMark = DateTime.Now;

        // Act
        writer.MarkInactive(1);

        // Assert
        Assert.True(_state.UpdatedAt >= beforeMark);
    }

    [Fact]
    public void MarkInactive_DoesNotAffectOtherEntries()
    {
        // Arrange
        var entry1 = new StateEntry { BackupId = 1, BackupName = "Test1", Status = BackupStatus.Active };
        var entry2 = new StateEntry { BackupId = 2, BackupName = "Test2", Status = BackupStatus.Active };
        _state.Entries[1] = entry1;
        _state.Entries[2] = entry2;
        var writer = CreateWriter();

        // Act
        writer.MarkInactive(1);

        // Assert
        Assert.Equal(BackupStatus.Inactive, _state.Entries[1].Status);
        Assert.Equal(BackupStatus.Active, _state.Entries[2].Status);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Update_ThenMarkInactive_WorksCorrectly()
    {
        // Arrange
        var writer = CreateWriter();
        var entry = new StateEntry
        {
            BackupId = 1,
            BackupName = "Test",
            Status = BackupStatus.Active
        };

        // Act
        writer.Update(entry);
        writer.MarkInactive(1);

        // Assert
        Assert.Equal(BackupStatus.Inactive, _state.Entries[1].Status);
        Assert.True(File.Exists(_testFilePath));
    }

    [Fact]
    public void PathProvider_GetStatePath_IsCalledOnEachWrite()
    {
        // Arrange
        var entry = new StateEntry { BackupId = 1, BackupName = "Test" };
        _state.Entries[1] = entry;
        var writer = CreateWriter();

        // Act
        writer.Update(new StateEntry { BackupId = 2, BackupName = "Test2" });
        writer.MarkInactive(1);

        // Assert
        _pathProviderMock.Verify(p => p.GetStatePath(), Times.Exactly(2));
    }

    #endregion
}
