namespace EasySave.State;

public class GlobalState
{
    public DateTime UpdatedAt { get; set; }
    public Dictionary<int, StateEntry> Entries { get; set; } = new();
}
