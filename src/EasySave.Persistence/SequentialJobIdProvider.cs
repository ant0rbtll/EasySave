using EasySave.Core;

namespace EasySave.Persistence;

public class SequentialJobIdProvider : IJobIdProvider
{
    public int NextId(List<SaveWork> existing)
    {
        return 0;
    }
}
