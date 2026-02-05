using EasySave.Core;

namespace EasySave.Persistence;

/// <summary>
/// Provides unique identifiers for backup jobs.
/// </summary>
public interface IJobIdProvider
{
    /// <summary>
    /// Generates the next available identifier.
    /// </summary>
    /// <param name="existing">List of existing jobs.</param>
    /// <returns>The next unique identifier.</returns>
    int NextId(List<BackupJob> existing);
}
