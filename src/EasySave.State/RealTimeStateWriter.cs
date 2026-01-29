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

        // Horodatage de la dernière action
        entry.timestamp = DateTime.Now;

        // Ajout ou mise à jour de l'entrée
        this.state.Entries[entry.backupId] = entry;

        // Mise à jour globale
        this.state.UpdatedAt = DateTime.Now;

        // Écriture immédiate dans le fichier state.json
        string path = this.pathProvider.GetStatePath();
        this.serializer.WritePrettyJson(path, this.state);
    }

    public void MarckInnactiv(int backupId)
    {
        if (!this.state.Entries.ContainsKey(backupId))
            return;

        var entry = this.state.Entries[backupId];

        entry.status = BackupStatus.Inactive;
        entry.timestamp = DateTime.Now;

        // Optionnel mais propre
        entry.currentSourcePath = string.Empty;
        entry.currentDestinationPath = string.Empty;
        entry.progressPercent = 100;
        entry.remainingFiles = 0;
        entry.remainingSizeBytes = 0;

        this.state.UpdatedAt = DateTime.Now;

        string path = this.pathProvider.GetStatePath();
        this.serializer.WritePrettyJson(path, this.state);
    }
    void GetDailyLogPath(DateTime date) 
    {
    }
    void GetStatePath()
    {
    }
    void GetJobsConfigPath()
    {
    }
}