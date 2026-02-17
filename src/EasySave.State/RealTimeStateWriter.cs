using EasySave.Configuration;

namespace EasySave.State;

public class RealTimeStateWriter(
    IPathProvider pathProvider,
    GlobalState state) : IStateWriter
{
    private readonly object _sync = new();

    /// <summary>
    /// Writes the state entry to the real-time state file.
    /// </summary>
    #region Update
    public void Update(StateEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_sync)
        {
            state.Entries[entry.BackupId] = entry;
            WriteStateFileLocked();
        }
    }
    #endregion

    /// <summary>
    /// Marks a backup entry as inactive in the real-time state file.
    /// </summary>
    #region MarkInactive
    public void MarkInactive(int backupId)
    {
        lock (_sync)
        {
            if (!state.Entries.TryGetValue(backupId, out var entry))
                return;

            entry.Status = BackupStatus.Inactive;
            entry.Timestamp = DateTime.Now;

            WriteStateFileLocked();
        }
    }
    #endregion

    private void WriteStateFileLocked()
    {
        state.UpdatedAt = DateTime.Now;
        string json = StateSerializer.ToPrettyJson(state);
        string path = pathProvider.GetStatePath();
        var tempPath = path + ".tmp";

        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}
