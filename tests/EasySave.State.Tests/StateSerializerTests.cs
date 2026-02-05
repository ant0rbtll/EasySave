using System.Text.Json;

namespace EasySave.State.Tests;

public class StateSerializerTests
{
    #region ToPrettyJson Tests

    [Fact]
    public void ToPrettyJson_NullState_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => StateSerializer.ToPrettyJson(null!));
    }

    [Fact]
    public void ToPrettyJson_EmptyState_ReturnsEmptyJsonObject()
    {
        // Arrange
        var state = new GlobalState { Entries = new Dictionary<int, StateEntry>() };

        // Act
        var json = StateSerializer.ToPrettyJson(state);

        // Assert
        Assert.NotNull(json);
        Assert.Equal("{}", json.Trim());
    }

    [Fact]
    public void ToPrettyJson_WithEntries_ReturnsValidJson()
    {
        // Arrange
        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>
            {
                { 1, new StateEntry { BackupId = 1, BackupName = "Test" } }
            }
        };

        // Act
        var json = StateSerializer.ToPrettyJson(state);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"1\"", json); // Key
        Assert.Contains("BackupId", json);
        Assert.Contains("BackupName", json);
        Assert.Contains("Test", json);
    }

    [Fact]
    public void ToPrettyJson_ReturnsIndentedJson()
    {
        // Arrange
        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>
            {
                { 1, new StateEntry { BackupId = 1, BackupName = "Test" } }
            }
        };

        // Act
        var json = StateSerializer.ToPrettyJson(state);

        // Assert - Indented JSON contains newlines
        Assert.Contains("\n", json);
        Assert.Contains("  ", json); // Indentation
    }

    [Fact]
    public void ToPrettyJson_MultipleEntries_SerializesAll()
    {
        // Arrange
        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>
            {
                { 1, new StateEntry { BackupId = 1, BackupName = "Backup1" } },
                { 2, new StateEntry { BackupId = 2, BackupName = "Backup2" } },
                { 3, new StateEntry { BackupId = 3, BackupName = "Backup3" } }
            }
        };

        // Act
        var json = StateSerializer.ToPrettyJson(state);

        // Assert
        Assert.Contains("Backup1", json);
        Assert.Contains("Backup2", json);
        Assert.Contains("Backup3", json);
    }

    [Fact]
    public void ToPrettyJson_SerializesAllStateEntryFields()
    {
        // Arrange
        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>
            {
                {
                    1,
                    new StateEntry
                    {
                        BackupId = 1,
                        BackupName = "TestBackup",
                        Status = BackupStatus.Active,
                        TotalFiles = 100,
                        TotalSizeBytes = 50000,
                        RemainingFiles = 50,
                        RemainingSizeBytes = 25000,
                        ProgressPercent = 50,
                        CurrentSourcePath = "/source/file.txt",
                        CurrentDestinationPath = "/dest/file.txt"
                    }
                }
            }
        };

        // Act
        var json = StateSerializer.ToPrettyJson(state);

        // Assert
        Assert.Contains("BackupId", json);
        Assert.Contains("BackupName", json);
        Assert.Contains("Status", json);
        Assert.Contains("TotalFiles", json);
        Assert.Contains("TotalSizeBytes", json);
        Assert.Contains("RemainingFiles", json);
        Assert.Contains("RemainingSizeBytes", json);
        Assert.Contains("ProgressPercent", json);
        Assert.Contains("CurrentSourcePath", json);
        Assert.Contains("CurrentDestinationPath", json);
    }

    [Fact]
    public void ToPrettyJson_CanBeDeserializedBack()
    {
        // Arrange
        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>
            {
                {
                    1,
                    new StateEntry
                    {
                        BackupId = 1,
                        BackupName = "TestBackup",
                        Status = BackupStatus.Active,
                        TotalFiles = 100,
                        ProgressPercent = 50
                    }
                }
            }
        };

        // Act
        var json = StateSerializer.ToPrettyJson(state);
        var deserialized = JsonSerializer.Deserialize<Dictionary<int, StateEntry>>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized!.TryGetValue(1, out var entry));
        Assert.Equal("TestBackup", entry.BackupName);
        Assert.Equal(BackupStatus.Active, entry.Status);
        Assert.Equal(100, entry.TotalFiles);
        Assert.Equal(50, entry.ProgressPercent);
    }

    [Fact]
    public void ToPrettyJson_WithNullStrings_SerializesCorrectly()
    {
        // Arrange
        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>
            {
                {
                    1,
                    new StateEntry
                    {
                        BackupId = 1,
                        BackupName = null,
                        CurrentSourcePath = null,
                        CurrentDestinationPath = null
                    }
                }
            }
        };

        // Act
        var json = StateSerializer.ToPrettyJson(state);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("null", json);
    }

    [Fact]
    public void ToPrettyJson_WithSpecialCharacters_SerializesCorrectly()
    {
        // Arrange
        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>
            {
                {
                    1,
                    new StateEntry
                    {
                        BackupId = 1,
                        BackupName = "Test \"Backup\" with 'quotes'",
                        CurrentSourcePath = "/path/with spaces/file.txt"
                    }
                }
            }
        };

        // Act
        var json = StateSerializer.ToPrettyJson(state);

        // Assert
        Assert.NotNull(json);
        // System.Text.Json encodes quotes as Unicode \u0022
        Assert.Contains("\\u0022Backup\\u0022", json);
        Assert.Contains("/path/with spaces/file.txt", json);
    }

    [Fact]
    public void ToPrettyJson_AllBackupStatuses_SerializeCorrectly()
    {
        // Arrange & Act & Assert
        Assert.All(Enum.GetValues<BackupStatus>(), status =>
        {
            var state = new GlobalState
            {
                Entries = new Dictionary<int, StateEntry>
                {
                    { 1, new StateEntry { BackupId = 1, Status = status } }
                }
            };

            var json = StateSerializer.ToPrettyJson(state);
            Assert.NotNull(json);
            Assert.Contains("Status", json);
        });
    }

    #endregion
}
