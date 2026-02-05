using EasySave.Core;

namespace EasySave.Persistence;

/// <summary>
/// ID provider that finds the smallest available ID.
/// </summary>
public class SequentialJobIdProvider : IJobIdProvider
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown if the maximum ID value has been reached.
    /// </exception>
    public int NextId(List<BackupJob> existing)
    {
        if (existing == null || existing.Count == 0)
            return 1;

        // Find the smallest available ID (gaps in the sequence)
        var existingIds = existing.Select(j => j.Id).Distinct().OrderBy(id => id).ToList();

        int nextId = 1;
        foreach (var id in existingIds)
        {
            if (id == nextId)
            {
                nextId++;
            }
            else
            {
                // Found a gap in the sequence
                return nextId;
            }
        }

        // No gap, return the next ID after max
        if (nextId == int.MaxValue)
            throw new InvalidOperationException("Cannot generate a new job ID because the maximum allowed ID value has been reached.");
        
        return nextId;
    }
}
