using EasySave.State.Tests.Mocks;

namespace EasySave.State.Tests;

public class RealTimeStateWriterTests
{
    [Fact]
    /// <summary>
    /// Appelle l'update lorsque le statue est mock "active"
    /// </summary>
    #region UpdateProgressActive
    public void Update_WhenProgressLessThan100_SetsStatusActive()
    {
        var pathProvider = new FakePathProvider();
        var filePath = pathProvider.GetStatePath();

        try
        {
            var state = new GlobalState
            {
                Entries = []
            };

            var writer = new RealTimeStateWriter(
                pathProvider,
                state);

            var entry = new StateEntry
            {
                BackupId = 1,
                Status = BackupStatus.Active,
                BackupName = "Test"
            };

            writer.Update(entry);

            Assert.Equal(BackupStatus.Active, entry.Status);
            Assert.True(state.Entries.ContainsKey(1));

            var json = File.ReadAllText(filePath);
            Assert.False(string.IsNullOrWhiteSpace(json));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
    #endregion

    [Fact]
    /// <summary>
    /// Appelle l'ecriture innactif lorsque le statue est mock "Inactive"
    /// </summary>
    #region MarkInactiveProgressInactive
    public void MarkInactive_WhenEntryExists_UpdatesTimestampAndWritesFile()
    {
        var pathProvider = new FakePathProvider();
        var filePath = pathProvider.GetStatePath();

        try
        {
            var state = new GlobalState
            {
                Entries = []
            };

            var entry = new StateEntry
            {
                BackupId = 1,
                Status = BackupStatus.Inactive,
                BackupName = "Test"
            };

            state.Entries.Add(1, entry);

            var writer = new RealTimeStateWriter(
                pathProvider,
                state);

            writer.MarkInactive(1);

            Assert.True(state.Entries.ContainsKey(1));
            Assert.NotEqual(DateTime.MinValue, entry.Timestamp);
            Assert.NotEqual(DateTime.MinValue, state.UpdatedAt);

            Assert.True(File.Exists(filePath));

            var json = File.ReadAllText(filePath);
            Assert.False(string.IsNullOrWhiteSpace(json));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
    #endregion
}

