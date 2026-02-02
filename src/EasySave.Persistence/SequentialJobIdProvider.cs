using EasySave.Core;

namespace EasySave.Persistence;

public class SequentialJobIdProvider : IJobIdProvider
{
    public int NextId(List<SaveWork> existing)
    {
        if (existing == null || existing.Count == 0)
            return 1;
        
        return existing.Max(j => j.Id) + 1;
    }
}
