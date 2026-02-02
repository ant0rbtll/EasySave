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
            Name = entry.backupName,
            Status = entry.status.ToString(),
            Timestamp = entry.timestamp
        }).ToList(); 

        var options = new JsonSerializerOptions
        {
            WriteIndented = true, 
            Converters = { new JsonStringEnumConverter() } 
        };

        string json = JsonSerializer.Serialize(entries, options);
        json = json.Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "");

        Console.WriteLine("=========== STATE FILE UPDATED ===========");
        Console.WriteLine(json);
        Console.WriteLine("==========================================");
        Console.WriteLine();

        return json;
    }
    #endregion
}
