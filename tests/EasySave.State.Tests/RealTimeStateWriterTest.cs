using EasySave.State;
public class RealTimeStateWriterTests
{
    [Fact]
    /// <summary>
    /// Appelle l'update lorsque le statue est mock "active"
    /// </summary>
    #region UpdateProgressActive
    public void Update_WhenProgressLessThan100_SetsStatusActive()
    {
        var pathProvider = new TestPathProvider(); 
        var serializer = new StateSerializer();
        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>()
        };

        var writer = new RealTimeStateWriter(
            pathProvider,
            serializer,
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

        var filePath = pathProvider.GetStatePath();

        var json = File.ReadAllText(filePath);
        Assert.False(string.IsNullOrWhiteSpace(json));

        File.Delete(filePath);
    }
    #endregion

    [Fact]
    /// <summary>
    /// Appelle l'écriture innactif lorsque le statue est mock "Inactive"
    /// </summary>
    #region MarkInactivProgressInactive
    public void MarkInactiv_WhenEntryExists_UpdatesTimestampAndWritesFile()
    {
        var pathProvider = new TestPathProvider();
        var serializer = new StateSerializer();

        var state = new GlobalState
        {
            Entries = new Dictionary<int, StateEntry>()
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
            serializer,
            state);

        writer.MarkInactiv(1);

        Assert.True(state.Entries.ContainsKey(1));
        Assert.NotEqual(DateTime.MinValue, entry.Timestamp);
        Assert.NotEqual(DateTime.MinValue, state.UpdatedAt);

        var filePath = pathProvider.GetStatePath();
        Assert.True(File.Exists(filePath));

        var json = File.ReadAllText(filePath);
        Assert.False(string.IsNullOrWhiteSpace(json));

        File.Delete(filePath);
    }
    #endregion
}

