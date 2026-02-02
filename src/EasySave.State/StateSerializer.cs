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

        var json = state.Entries.Values.Select(entry => new
        {
            Name = entry.backupName,
            Status = entry.status.ToString(),
            Timestamp = entry.timestamp
        });

        string jobState = json.ToString();
         jobState = jobState.Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "");

        Console.WriteLine("=========== STATE FILE UPDATED ===========");
        Console.WriteLine(jobState);
        Console.WriteLine("==========================================");
        Console.WriteLine();

        return json.ToString();
    }
    #endregion
}
