using EasySave.Core;

namespace EasySave.Persistence;

public interface IJobIdProvider
{
    int NextId(List<SaveWork> existing);
}
