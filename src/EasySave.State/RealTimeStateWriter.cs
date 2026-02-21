using EasySave.Configuration;
using EasySave.Core.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySave.State;

public class RealTimeStateWriter(
    IPathProvider pathProvider,
    GlobalState state) : IStateWriter
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: true)
        }
    };

    private readonly object _sync = new();
    private bool _isStateInitialized;

    /// <summary>
    /// Writes the state entry to the real-time state file.
    /// </summary>
    #region Update
    public void Update(StateEntry entry)
    {
        EasysaveDefaultException.ThrowIfNull(entry);

        lock (_sync)
        {
            EnsureStateInitializedLocked();
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
            EnsureStateInitializedLocked();
            if (!state.Entries.TryGetValue(backupId, out var entry))
                return;

            entry.Status = BackupStatus.Inactive;
            entry.Timestamp = DateTime.Now;

            WriteStateFileLocked();
        }
    }
    #endregion

    private void EnsureStateInitializedLocked()
    {
        if (_isStateInitialized)
            return;

        _isStateInitialized = true;

        string path = pathProvider.GetStatePath();
        if (!File.Exists(path))
            return;

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException)
        {
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            var existingEntries = JsonSerializer.Deserialize<Dictionary<int, StateEntry>>(json, s_jsonOptions);
            if (existingEntries is null)
                return;

            foreach (var (id, entry) in existingEntries)
            {
                state.Entries[id] = entry;
            }
        }
        catch (JsonException)
        {
            // Ignore invalid persisted state and continue with in-memory entries only.
        }
    }

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
