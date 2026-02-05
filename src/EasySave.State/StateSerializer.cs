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
    #region ToPrettyJson
    public static string ToPrettyJson(GlobalState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        string json = JsonSerializer.Serialize(state.Entries, s_jsonOptions);
        
        return json;
    }
    #endregion
}
