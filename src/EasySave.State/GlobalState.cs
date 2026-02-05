namespace EasySave.State;

/// <summary>
/// Represents the global real-time state of all backup operations.
/// </summary>
public class GlobalState
{
    /// <summary>
    /// Gets or sets the timestamp of the last state update.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the state entries indexed by backup job ID.
    /// </summary>
    public Dictionary<int, StateEntry> Entries { get; set; } = new();
}
