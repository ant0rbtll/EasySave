using EasySave.Configuration;

namespace EasySave.State;

public class RealTimeStateWriter(
    IPathProvider pathProvider,
    GlobalState state) : IStateWriter
{
    /// <summary>
    /// Lancement de l'ecriture dans le fichier d'ETR
    /// </summary>
    #region Update
    public void Update(StateEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        entry.Timestamp = DateTime.Now;
        state.Entries[entry.BackupId] = entry;

        WriteStateFile();
    }
    #endregion

    /// <summary>
    /// Lancement de l'ecriture dans le fichier d'ETR en cas d'innactivite
    /// </summary>
    #region MarkInactive
    public void MarkInactive(int backupId)
    {
        if (!state.Entries.TryGetValue(backupId, out var entry))
            return;

        entry.Timestamp = DateTime.Now;

        WriteStateFile();
    }
    #endregion

    private void WriteStateFile()
    {
        state.UpdatedAt = DateTime.Now;
        string json = StateSerializer.WritePrettyJson(state);
        string path = pathProvider.GetStatePath();
        File.WriteAllText(path, json);
    }
}
