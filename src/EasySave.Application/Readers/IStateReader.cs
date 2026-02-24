using EasySave.State;

namespace EasySave.Application.Readers;

/// <summary>
/// Reads the runtime backup state from persistence.
/// </summary>
public interface IStateReader
{
    /// <summary>
    /// Returns state entries indexed by backup job id.
    /// </summary>
    IReadOnlyDictionary<int, StateEntry> ReadEntries();
}
