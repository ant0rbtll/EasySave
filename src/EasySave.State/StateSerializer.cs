using System.Text.Json.Serialization;
using System.Text.Json;

namespace EasySave.State;

public class StateSerializer
{
    /// <summary>
    /// renvoie un json indenté 
    /// </summary>
    #region WritePrettyJson
    public string WritePrettyJson(GlobalState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        var entries = state.Entries.Values.Select(entry => new
        {
            Name = entry.BackupName,
            Status = entry.Status.ToString(),
            Timestamp = entry.Timestamp
        }).ToList(); 

        var options = new JsonSerializerOptions
        {
            WriteIndented = true, 
            Converters = { new JsonStringEnumConverter() } 
        };

        string json = JsonSerializer.Serialize(entries, options);
        
        return json;
    }
    #endregion
}
