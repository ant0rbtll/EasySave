using EasySave.Configuration;

namespace EasySave.State;

public class RealTimeStateWriter(
    IPathProvider pathProvider,
    GlobalState state) : IStateWriter
{
    /// <summary>
    /// Writes the state entry to the real-time state file.
    /// </summary>
    #region Update
    public void Update(StateEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        state.Entries[entry.BackupId] = entry;

        WriteStateFile();
    }
    #endregion

    /// <summary>
    /// Marks a backup entry as inactive in the real-time state file.
    /// </summary>
    #region MarkInactive
    public void MarkInactive(int backupId)
    {
        if (!state.Entries.TryGetValue(backupId, out var entry))
            return;

        entry.Status = BackupStatus.Inactive;
        entry.Timestamp = DateTime.Now;

        WriteStateFile();
    }
    #endregion

    private void WriteStateFile()
    {
        state.UpdatedAt = DateTime.Now;
        string json = StateSerializer.ToPrettyJson(state);
        string path = pathProvider.GetStatePath();
        File.WriteAllText(path, json);
    }
}
