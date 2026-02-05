namespace EasySave.State;

/// <summary>
/// Writes backup state information to the real-time state file.
/// </summary>
public interface IStateWriter
{
    /// <summary>
    /// Updates the state entry for a specific backup job.
    /// </summary>
    /// <param name="entry">The state entry to write.</param>
    void Update(StateEntry entry);

    /// <summary>
    /// Marks a backup entry as inactive.
    /// </summary>
    /// <param name="backupId">The identifier of the backup job to mark as inactive.</param>
    void MarkInactive(int backupId);
}
