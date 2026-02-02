using EasySave.Configuration;

namespace EasySave.State;

public class RealTimeStateWriter : IStateWriter
{
    private readonly StateSerializer serializer;
    private readonly IPathProvider pathProvider;
    private readonly GlobalState state;

    public RealTimeStateWriter(
        IPathProvider pathProvider,
        StateSerializer serializer,
        GlobalState state)
    {
        this.pathProvider = pathProvider;
        this.serializer = serializer;
        this.state = state;
    }

    /// <summary>
    /// Lancement de l'écriture dans le fichier d'ETR
    /// </summary>
    #region Update
    public void Update(StateEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        entry.timestamp = DateTime.Now;

        if (entry.progressPercent < 100)
        {
            entry.status = BackupStatus.Active;
        }
        else
        {
            entry.status = BackupStatus.Done;
        }

        state.Entries[entry.backupId] = entry;
        state.UpdatedAt = DateTime.Now;

        string json = serializer.WritePrettyJson(state);

        string path = pathProvider.GetStatePath();
        File.WriteAllText(path, json);
    }
    #endregion

    /// <summary>
    /// Lancement de l'écriture dans le fichier d'ETR en cas d'innactivité
    /// </summary>
    #region MarkInactiv
    public void MarkInactiv(int backupId)
    {
        if (!state.Entries.TryGetValue(backupId, out var entry))
            return;

        entry.status = BackupStatus.Inactive;
        entry.timestamp = DateTime.Now;

        state.UpdatedAt = DateTime.Now;

        string json = serializer.WritePrettyJson(state);

        string path = pathProvider.GetStatePath();
        File.WriteAllText(path, json);
    }
    #endregion
}
