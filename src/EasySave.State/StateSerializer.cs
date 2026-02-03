using System.Text.Json;

namespace EasySave.State;

public class StateSerializer
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Returns an indented JSON string. 
    /// </summary>
    #region WritePrettyJson
    public static string WritePrettyJson(GlobalState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var entries = state.Entries.Values.Select(entry => new
        {
            Name = entry.BackupName,
            Status = entry.Status.ToString(),
            entry.Timestamp
        }).ToList(); 

        string json = JsonSerializer.Serialize(entries, s_jsonOptions);
        
        return json;
    }
    #endregion
}
