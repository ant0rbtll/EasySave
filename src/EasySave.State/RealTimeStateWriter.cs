using EasySave.State.Configuration.Paths;

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

    public void Update(StateEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        // Horodatage de la mise à jour
        entry.timestamp = DateTime.Now;

        // Détermination du statut à partir du pourcentage
        if (entry.progressPercent <= 0)
        {
            entry.status = BackupStatus.Active;
        }
        else if (entry.progressPercent < 100)
        {
            entry.status = BackupStatus.Active;
        }
        else
        {
            entry.status = BackupStatus.Done;
        }

        // Ajout ou mise à jour de la sauvegarde dans le GlobalState
        state.Entries[entry.backupId] = entry;
        state.UpdatedAt = DateTime.Now;

        // Appel unique au serializer (écriture + console)
        serializer.WritePrettyJson(
            pathProvider.GetStatePath(),
            state
        );
    }

    public void MarckInnactiv(int backupId)
    {
        if (!state.Entries.TryGetValue(backupId, out var entry))
            return;

        // Passage explicite à Inactive
        entry.status = BackupStatus.Inactive;
        entry.timestamp = DateTime.Now;

        state.UpdatedAt = DateTime.Now;

        // Appel unique au serializer (écriture + console)
        serializer.WritePrettyJson(
            pathProvider.GetStatePath(),
            state
        );
    }
}
