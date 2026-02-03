using EasySave.Core;

namespace EasySave.Persistence;

public class SequentialJobIdProvider : IJobIdProvider
{
    public int NextId(List<BackupJob> existing)
    {
        if (existing == null || existing.Count == 0)
            return 1;
        
        var maxId = existing.Max(j => j.Id);
        if (maxId == int.MaxValue)
            throw new InvalidOperationException("Cannot generate a new job ID because the maximum allowed ID value has been reached.");
        return maxId + 1;
    }
}
